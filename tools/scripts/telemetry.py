#!/usr/bin/env python3
"""Decode local Voar telemetry; list/pull Quest captures using the app's logged path."""
import argparse
import csv
import json
import math
from pathlib import Path
import re
import shlex
import statistics
import struct
import subprocess
import sys

MAGIC=b'VOARTLM1'
EVENTS='SessionStart SessionEnd CalibrationAccepted CalibrationRejected Recenter TrackingLost TrackingRecovered ViewChanged WeatherChanged StallEntry StallRecovery ThermalEntry ThermalExit Collision LandingAttempt LandingSuccess Takeoff StreamingStall StreamingRecovered CharacterReturn Marker Paused Resumed FlightReset SupportLost ControlModeChanged TrickCompleted ObjectiveStarted ObjectiveProgress ObjectiveCompleted ObjectiveFailed RegionChanged RestStarted RestEnded CollectibleCaught EffortSummary StallProtectionLost StallProtectionRecovered'.split()
ROOT=Path(__file__).resolve().parents[2]


def decode(path):
    frames=[]; events=[]; complete=False; dropped=None; truncated=False
    with Path(path).open('rb') as f:
        if f.read(8)!=MAGIC: raise ValueError('Not a Voar telemetry file')
        head=f.read(8)
        if len(head)!=8: raise ValueError('Truncated header')
        version,length=struct.unpack('<ii',head)
        if version not in (1,2,3): raise ValueError(f'Unsupported schema {version}')
        if not 0<length<=1024*1024: raise ValueError('Invalid header size')
        header=json.loads(f.read(length)); names=header['fields']
        if len(names)>4096 or len(names)!=len(set(names)): raise ValueError('Invalid fields')
        wide=header.get('wideFields',[])
        if version==3 and (not isinstance(wide,list) or any(k not in names for k in wide)): raise ValueError('Invalid wide fields')
        layout=struct.Struct('<'+''.join('d' if version<3 or k in wide else 'f' for k in names))
        while prefix:=f.read(5):
            if len(prefix)!=5: truncated=True; break
            kind,size=struct.unpack('<Bi',prefix)
            if not 0<=size<=1024*1024: raise ValueError('Invalid record size')
            payload=f.read(size)
            if len(payload)!=size: truncated=True; break
            if kind==1:
                if size!=layout.size: raise ValueError('Frame/schema size mismatch')
                frames.append(dict(zip(names,layout.unpack(payload))))
            elif kind==2:
                if size!=20: raise ValueError('Invalid event size')
                code,time,value=struct.unpack('<idd',payload)
                events.append(dict(event=EVENTS[code-1] if 1<=code<=len(EVENTS) else f'Unknown{code}',timestamp=time,value=value))
            elif kind==3:
                if size!=4: raise ValueError('Invalid footer')
                dropped=struct.unpack('<i',payload)[0]; complete=True
    return dict(header=header,frames=frames,events=events,complete=complete,dropped=dropped,truncated=truncated)


def percentile(values,p):
    v=sorted(x for x in values if math.isfinite(x))
    if not v:return None
    i=(len(v)-1)*p; a=int(i); b=min(a+1,len(v)-1)
    return v[a]+(v[b]-v[a])*(i-a)


def stats(values):
    v=[x for x in values if math.isfinite(x)]
    return dict(n=len(v),p05=percentile(v,.05),p50=percentile(v,.5),p95=percentile(v,.95),p99=percentile(v,.99),minimum=min(v) if v else None,maximum=max(v) if v else None)


def magnitude(row,prefix):return math.sqrt(sum(row.get(prefix+'_'+c,math.nan)**2 for c in 'xyz'))


def body_relative(row,side):
    # Rotate tracking-space hand minus head by inverse torso quaternion.
    v=[row['raw_'+side+'_position_'+c]-row['raw_head_position_'+c] for c in 'xyz']
    q=[-row['raw_body_rotation_'+c] for c in 'xyz']+[row['raw_body_rotation_w']]
    x,y,z=v; a,b,c,w=q
    tx,ty,tz=2*(b*z-c*y),2*(c*x-a*z),2*(a*y-b*x)
    return [x+w*tx+b*tz-c*ty,y+w*ty+c*tx-a*tz,z+w*tz+a*ty-b*tx]


def relative_quaternion(q,heading):
    x,y,z,w=q; a,b,c,d=[-v for v in heading[:3]]+[heading[3]]
    return [d*x+a*w+b*z-c*y,d*y-a*z+b*w+c*x,d*z+a*y-b*x+c*w,d*w-a*x-b*y-c*z]


def wrist_excursion(row,side):
    def q(prefix):return [row[prefix+'_'+c] for c in 'xyzw']
    current=relative_quaternion(q('raw_'+side+'_rotation'),q('raw_body_rotation'))
    neutral=relative_quaternion(q('calibration_'+side+'_rotation'),q('calibration_heading'))
    dot=abs(sum(a*b for a,b in zip(current,neutral)))
    return math.degrees(2*math.acos(min(1,max(0,dot))))


def summarize(data):
    rows=data['frames']; duration=sum(r['dt'] for r in rows)
    result=dict(utc=data['header'].get('utc'),build=data['header'].get('git'),session=data['header'].get('session'),character=data['header'].get('character'),frames=len(rows),simulation_seconds=duration,clean_close=data['complete'],truncated=data['truncated'],dropped=data['dropped'],allocation_counter='unavailable',metrics={},inputs={},events={})
    for key in ['airspeed','groundspeed','aoa','head_pitch','energy','render_dt','unscaled_dt','cpu_ms','gpu_ms','clearance','reach_left','reach_right','generation_ms','capture_cpu_ms']:
        result['metrics'][key]=stats([r.get(key,math.nan) for r in rows])
    for key in ['lift','drag','stroke','wind']:
        result['metrics'][key+'_magnitude']=stats([magnitude(r,key) for r in rows])
    result['metrics']['climb_mps']=stats([r['velocity_y'] for r in rows])
    result['phase_fraction']={str(phase):sum(r['dt'] for r in rows if r['phase']==phase)/duration if duration else 0 for phase in range(7)}
    result['slow_frames']={str(hz)+'hz':sum(r['unscaled_dt']>1/hz for r in rows) for hz in (72,90,120)}
    for side in ['left','right']:
        valid=[r for r in rows if r['raw_'+side+'_tracked'] and r['raw_head_tracked'] and r['raw_body_tracked']]
        positions=[body_relative(r,side) for r in valid]
        strokes=[];last=None; armed=False
        # Count robust positive-to-negative vertical velocity reversals, separated by
        # 0.25 s. This is a human cadence estimate, not a measured bird wingbeat rate.
        for r in valid:
            vy=r['raw_'+side+'_velocity_y'];t=r['timestamp']
            if vy>.3:armed=True
            if armed and vy<-.3:
                if last is not None and .25<t-last<3:strokes.append(1/(t-last))
                last=t;armed=False
        result['inputs'][side]=dict(body_relative_m={c:stats([p[i] for p in positions]) for i,c in enumerate('xyz')},human_stroke_hz=stats(strokes),finite_difference_speed=stats([magnitude(r,'raw_'+side+'_velocity') for r in valid]),native_speed=stats([magnitude(r,'native_'+side+'_velocity') for r in valid if r['native_'+side+'_velocity_available']]),native_angular_radians_per_second=stats([magnitude(r,'native_'+side+'_angular') for r in valid if r['native_'+side+'_angular_available']]),tracking_fraction=len(valid)/len(rows) if rows else 0)
    for e in data['events']:result['events'][e['event']]=result['events'].get(e['event'],0)+1
    result['markers']=[e for e in data['events'] if e['event']=='Marker']
    result['state_seconds']={name:sum(r['dt'] for r in rows if predicate(r)) for name,predicate in {
        'stall':lambda r:r.get('stalled',0)>0,
        'rising_air':lambda r:r['wind_y']>2,
        'descending_air':lambda r:r['wind_y']<-2,
        'streaming_blocked':lambda r:r['streaming_blocked']>0,
        'tracking_incomplete':lambda r:not(r['raw_left_tracked'] and r['raw_right_tracked'] and r['raw_head_tracked'])}.items()}
    def quaternion(r,p):return [r[p+'_'+c] for c in 'xyzw']
    def angle(a,b):
        dot=abs(sum(x*y for x,y in zip(a,b)));return math.degrees(2*math.acos(min(1,max(0,dot))))
    result['inputs']['asymmetry_speed_mps']=stats([abs(magnitude(r,'raw_left_velocity')-magnitude(r,'raw_right_velocity')) for r in rows if r['raw_left_tracked'] and r['raw_right_tracked']])
    for side in ('left','right'):
        result['inputs'][side]['wrist_deviation_degrees']=stats([wrist_excursion(r,side) for r in rows if r['raw_'+side+'_tracked'] and r['raw_body_tracked'] and r['calibrated']])
        yr=result['inputs'][side]['body_relative_m']['y']
        result['inputs'][side]['robust_stroke_height_m']=yr['p95']-yr['p05'] if yr['n'] else None
    yaws=[];last=None;unwrapped=0
    for r in rows:
        x,y,z,w=quaternion(r,'raw_body_rotation');yaw=math.degrees(math.atan2(2*(w*y+x*z),1-2*(y*y+x*x)))
        if last is None:unwrapped=yaw
        else:unwrapped+=(yaw-last+180)%360-180
        yaws.append(unwrapped);last=yaw
    result['inputs']['torso_yaw_unwrapped_degrees']=stats(yaws)
    glides=[];segment=[]
    def close_segment():
        if len(segment)>1:
            a,b=segment[0],segment[-1];seconds=sum(r['dt'] for r in segment)
            distance=math.hypot(b['logical_x']-a['logical_x'],b['logical_z']-a['logical_z']);drop=a['logical_y']-b['logical_y']
            glides.append(dict(seconds=seconds,horizontal_m=distance,height_change_m=-drop,glide_ratio=distance/drop if drop>.01 else None,energy_change_j=b['energy']-a['energy'],mean_vertical_air_mps=sum(r['wind_y']*r['dt'] for r in segment)/seconds))
        segment.clear()
    for r in rows:
        if r['phase']!=0 or segment and (r['simulation_time']<segment[-1]['simulation_time'] or r['timestamp']-segment[-1]['timestamp']>.15):close_segment()
        if r['phase']==0:segment.append(r)
    close_segment();result['glide_segments']=glides
    result['landing_approach_speed_mps']=stats([e['value'] for e in data['events'] if e['event']=='LandingAttempt'])
    result['flight_game']={name:stats([r[name] for r in rows if name in r]) for name in ('control_mode','angular_velocity_x','angular_velocity_y','angular_velocity_z','trick_score','thermal_assist_bank','objective_progress','mission_score','island_distance')}
    if rows and 'effort_active_seconds' in rows[-1]:
        last=rows[-1]
        result['effort_proxies']={k:last[k] for k in ('effort_active_seconds','effort_rest_seconds','effort_hand_travel_m','effort_strokes','food_caught','food_points')}
        result['effort_proxies']['not_medical']='Movement proxies; no calories, fitness or fatigue diagnosis'
        result['effort_proxies']['coverage_seconds']={
            'clock':'recorded capped simulation dt; categories can overlap',
            'perched':sum(r['dt'] for r in rows if r['phase']==5),
            'paused':sum(r['dt'] for r in rows if r['phase']==6),
            'tracking_incomplete':sum(r['dt'] for r in rows if not all(r[k] for k in ('raw_head_tracked','raw_left_tracked','raw_right_tracked'))),
            'streaming_blocked':sum(r['dt'] for r in rows if r.get('streaming_blocked',0)),
        }
        result['effort_proxies']['span_ratio']=stats([r['span_ratio'] for r in rows if r['input_wings_enabled']])
        result['effort_proxies']['rest_bouts']=sum(e['event']=='RestStarted' for e in data['events'])
        result['effort_proxies']['rising_but_sinking_seconds']=sum(r['dt'] for r in rows if r['wind_y']>2 and r['velocity_y']<0 and r['phase'] not in (5,6))
    result['limits']=['Cadence inferred from thresholded human controller motion; inspect raw traces.', 'CPU/GPU NaN means unsupported; no zero-allocation claim.', 'Frame timings may describe earlier rendered frames.', 'Point-mass replay excludes streamed collisions and mutable wind unless supplied.']
    return result


def markdown(summary):
    lines=[f"# Flight session {summary['session']}",f"\nCharacter: {summary['character']}; {summary['frames']} frames; {summary['simulation_seconds']:.2f} simulated seconds.",f"Clean close: {summary['clean_close']}; dropped: {summary['dropped']}; truncated: {summary['truncated']}.", '\n| Metric | p50 | p95 | p99 |', '|---|---:|---:|---:|']
    def fmt(x):return 'unavailable' if x is None else f'{x:.4g}'
    for name,s in summary['metrics'].items():lines.append('| '+name+' | '+' | '.join(fmt(s[k]) for k in ['p50','p95','p99'])+' |')
    lines+=['\nEvents: '+json.dumps(summary['events']), '\nInput distributions and markers are in the adjacent JSON report.','\n'+'\n'.join('- '+x for x in summary['limits'])]
    return '\n'.join(lines)+'\n'


def extract_window(data,number,before=5,after=10):
    marker=next((e for e in data['events'] if e['event']=='Marker' and e['value']==number),None)
    if marker is None:raise ValueError(f'Marker {number} not found')
    start,end=marker['timestamp']-before,marker['timestamp']+after
    # Deliberately preserves only raw input, calibration and original time step.
    frames=[{k:v for k,v in r.items() if k.startswith(('raw_','calibration_')) or k in ('dt','timestamp','calibrated','human_span','bird_half_span','motion_scale','input_wings_enabled')} for r in data['frames'] if start<=r['timestamp']<=end]
    return dict(schemaVersion=data['header'].get('schemaVersion',1),source_session=data['header']['session'],marker=number,header=data['header'],frames=frames,events=[e for e in data['events'] if start<=e['timestamp']<=end])


def device_path(adb,serial,override=None):
    if override: path=override
    else:
        log=subprocess.run([adb,'-s',serial,'logcat','-d','-s','Unity:I','*:S'],check=True,capture_output=True,text=True).stdout
        matches=re.findall(r'VOAR_TELEMETRY_PATH=(/[^\r\n]+?/telemetry)/[0-9a-f]+\.voartlm',log)
        if not matches:raise ValueError('No app-reported telemetry path in logcat. Launch development build or pass its printed --remote-path.')
        path=matches[-1]
    if not path.startswith('/') or not path.endswith('/telemetry') or '\n' in path:raise ValueError('Invalid telemetry directory')
    check=subprocess.run([adb,'-s',serial,'shell','test -d '+shlex.quote(path)],capture_output=True)
    if check.returncode:raise ValueError('Device telemetry directory not readable: '+path)
    return path


def cached_summary(path):
    """Reuse analysis only while the recording and analysis implementation match."""
    path=Path(path); stat=path.stat(); cache=path.with_suffix('.history-cache.json')
    stamp=[stat.st_size,stat.st_mtime_ns,Path(__file__).stat().st_mtime_ns]
    try:
        saved=json.loads(cache.read_text())
        if saved.get('stamp')==stamp:return saved['summary']
    except (OSError,ValueError,KeyError):pass
    result=summarize(decode(path))
    cache.write_text(json.dumps(dict(stamp=stamp,summary=result),allow_nan=False)+'\n')
    return result


def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('command',choices=['list','pull','summarize','export-csv','markers','history']);p.add_argument('file',nargs='?');p.add_argument('--output',type=Path);p.add_argument('--remote-path');p.add_argument('--serial');p.add_argument('--marker',type=int);p.add_argument('--before',type=float,default=5);p.add_argument('--after',type=float,default=10)
    a=p.parse_args(argv)
    if a.command=='history':
        if not a.file:p.error('history requires a local recording directory')
        results=[];seen=set()
        for path in sorted(Path(a.file).rglob('*.voartlm')):
            result=cached_summary(path);identity=result.get('session',str(path))
            if identity in seen:continue
            seen.add(identity);results.append(result)
        results.sort(key=lambda s:s.get('utc') or '')
        out=a.output or Path(a.file)/'history.json';out.write_text(json.dumps(results,indent=2,allow_nan=False)+'\n')
        lines=['# Recorded flight history','', 'Valid airborne simulation-time proxies; pause, perching and tracking gaps are excluded. Compare like species/modes/builds. Quiet hands are not measured medical recovery.','', '| UTC | Bird | Minutes | Active / quiet seconds | Strokes | Food points | Rising but sinking seconds |','|---|---|---:|---|---:|---:|---:|']
        for result in results:
            e=result.get('effort_proxies',{})
            lines.append('| '+str(result.get('utc'))+' | '+str(result.get('character'))+' | '+str(round(result['simulation_seconds']/60,1))+' | '+str(e.get('effort_active_seconds','unavailable'))+' / '+str(e.get('effort_rest_seconds','unavailable'))+' | '+str(e.get('effort_strokes','unavailable'))+' | '+str(e.get('food_points','unavailable'))+' | '+str(e.get('rising_but_sinking_seconds','unavailable'))+' |')
        out.with_suffix('.md').write_text('\n'.join(lines)+'\n');print(out);return
    if a.command in ('list','pull'):
        import quest
        adb=quest.require_adb(); serial=a.serial or quest.current_device(adb); remote=device_path(adb,serial,a.remote_path)
        files=subprocess.run([adb,'-s',serial,'shell','ls -1 '+shlex.quote(remote)],check=True,capture_output=True,text=True).stdout.splitlines()
        files=[f for f in files if re.fullmatch(r'[0-9a-f]{32}\.voartlm',f)]
        if a.command=='list':print('\n'.join(files));return
        selected=files if a.file in (None,'all') else [a.file]
        for name in selected:
            if name not in files:raise ValueError('Unknown session '+name)
            dest=a.output or ROOT/'artifacts/telemetry'/Path(name).stem;dest.mkdir(parents=True,exist_ok=True)
            subprocess.run([adb,'-s',serial,'pull',remote+'/'+name,str(dest/name)],check=True)
        return
    if not a.file:p.error('local telemetry file required')
    data=decode(a.file)
    if a.command=='summarize':
        result=summarize(data);out=a.output or Path(a.file).with_suffix('.summary.json');out.write_text(json.dumps(result,indent=2,allow_nan=False)+'\n');out.with_suffix('.md').write_text(markdown(result));print(out)
    elif a.command=='export-csv':
        out=a.output or Path(a.file).with_suffix('.csv')
        with out.open('w',newline='') as f:
            w=csv.DictWriter(f,fieldnames=data['header']['fields']);w.writeheader();w.writerows(data['frames'])
        print(out)
    else:
        if a.marker is None:print(json.dumps([e for e in data['events'] if e['event']=='Marker'],indent=2))
        else:
            result=extract_window(data,a.marker,a.before,a.after);out=a.output or Path(a.file).with_suffix(f'.marker-{a.marker}.json');out.write_text(json.dumps(result,indent=2)+'\n');print(out)

if __name__=='__main__':
    try:main()
    except (ValueError,OSError,subprocess.CalledProcessError) as e:sys.exit(str(e))
