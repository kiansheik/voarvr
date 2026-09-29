import json
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch
import telemetry

class TelemetryTests(unittest.TestCase):
    def test_history_cache_reuses_and_invalidates_changed_recording(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'session.voartlm';p.write_bytes(b'first')
            with patch.object(telemetry,'decode',return_value={}) as decode, patch.object(telemetry,'summarize',return_value={'session':'one'}):
                self.assertEqual(telemetry.cached_summary(p),{'session':'one'})
                telemetry.cached_summary(p);self.assertEqual(decode.call_count,1)
                p.write_bytes(b'longer recording');telemetry.cached_summary(p)
                self.assertEqual(decode.call_count,2)
    def fixture(self,tail=b'',version=1):
        header=json.dumps({'fields':['dt','timestamp'],'session':'test'}).encode()
        return b'VOARTLM1'+struct.pack('<ii',version,len(header))+header+struct.pack('<Bi2d',1,16,.011,12)+tail
    def decode(self,blob):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'sample.voartlm';p.write_bytes(blob);return telemetry.decode(p)
    def test_complete_and_marker(self):
        tail=struct.pack('<Biidd',2,20,21,12,3)+struct.pack('<Bii',3,4,7)
        d=self.decode(self.fixture(tail));self.assertTrue(d['complete']);self.assertEqual(d['dropped'],7)
        self.assertEqual(d['events'][0]['event'],'Marker');self.assertAlmostEqual(d['frames'][0]['dt'],.011)
        self.assertEqual(len(telemetry.extract_window(d,3)['frames']),1)
    def test_version_two_event_contract(self):
        d=self.decode(self.fixture(struct.pack("<Biidd",2,20,26,12,1),version=2))
        self.assertEqual(d["events"][0]["event"],"ControlModeChanged")
        self.assertEqual(len(d["frames"]),1)
    def test_compact_v3_preserves_wide_time_and_reads_float_input(self):
        h=json.dumps({'fields':['dt','timestamp','span_ratio'],'wideFields':['timestamp']}).encode()
        blob=b'VOARTLM1'+struct.pack('<ii',3,len(h))+h+struct.pack('<Bifdf',1,16,.0138889,987654321.125,.92)
        d=self.decode(blob);self.assertEqual(d['frames'][0]['timestamp'],987654321.125)
        self.assertAlmostEqual(d['frames'][0]['span_ratio'],.92,places=6)
    def test_compact_v4_preserves_motion_provenance_and_marker_window(self):
        fields=['dt','timestamp','raw_ground_turn','mapped_ground_turn',
                'raw_left_motion_estimated','raw_right_motion_estimated',
                'mapped_left_motion_estimated','mapped_right_motion_estimated']
        h=json.dumps({'schemaVersion':4,'fields':fields,'wideFields':['timestamp'],'session':'v4'}).encode()
        payload=struct.pack('<fd6f',.02,987654321.125,.75,-.25,1,0,0,1)
        blob=b'VOARTLM1'+struct.pack('<ii',4,len(h))+h+struct.pack('<Bi',1,len(payload))+payload
        blob+=struct.pack('<Biidd',2,20,21,987654321.125,2)+struct.pack('<Bii',3,4,0)
        d=self.decode(blob);frame=d['frames'][0]
        self.assertEqual(frame['timestamp'],987654321.125)
        self.assertEqual([frame[k] for k in fields[2:]],[.75,-.25,1,0,0,1])
        self.assertTrue(d['complete']);self.assertEqual(d['dropped'],0)
        window=telemetry.extract_window(d,2)
        self.assertEqual(window['schemaVersion'],4)
        self.assertEqual(window['frames'][0]['raw_ground_turn'],.75)
        self.assertEqual(window['frames'][0]['raw_left_motion_estimated'],1)
        self.assertEqual(window['frames'][0]['raw_right_motion_estimated'],0)
    def test_compact_v4_rejects_unknown_wide_field(self):
        h=json.dumps({'fields':['dt'],'wideFields':['missing']}).encode()
        blob=b'VOARTLM1'+struct.pack('<ii',4,len(h))+h
        with self.assertRaisesRegex(ValueError,'Invalid wide fields'):self.decode(blob)
    def test_compact_v5_preserves_hand_source_and_capture_clock_precision(self):
        fields=['timestamp','raw_left_pose_source','raw_left_sample_timestamp',
                'raw_left_motion_estimated','raw_left_unextrapolated_available',
                'raw_left_unextrapolated_position_x','raw_left_unextrapolated_timestamp',
                'raw_right_pose_source','raw_right_unextrapolated_available',
                'raw_right_unextrapolated_timestamp','hand_wmm2_enabled','hand_fmm_requested']
        wide=['timestamp','raw_left_sample_timestamp','raw_left_unextrapolated_timestamp',
              'raw_right_unextrapolated_timestamp']
        capture_time=987654321.1234567
        values=[capture_time+.03,1,capture_time+.02,0,1,-.45,capture_time,3,0,float('nan'),1,0]
        header={'schemaVersion':5,'fields':fields,'wideFields':wide,'session':'hands','input':'Meta hand tracking'}
        h=json.dumps(header).encode()
        layout='<'+''.join('d' if name in wide else 'f' for name in fields)
        payload=struct.pack(layout,*values)
        blob=b'VOARTLM1'+struct.pack('<ii',5,len(h))+h+struct.pack('<Bi',1,len(payload))+payload
        blob+=struct.pack('<Biidd',2,20,21,capture_time+.03,7)+struct.pack('<Bii',3,4,0)
        decoded=self.decode(blob);row=decoded['frames'][0]
        self.assertTrue(decoded['complete'])
        self.assertEqual(row['raw_left_sample_timestamp'],capture_time+.02)
        self.assertEqual(row['raw_left_unextrapolated_timestamp'],capture_time)
        self.assertEqual(row['raw_left_pose_source'],1)
        self.assertEqual(row['raw_right_pose_source'],3)
        self.assertEqual(row['raw_right_unextrapolated_available'],0)
        self.assertTrue(telemetry.math.isnan(row['raw_right_unextrapolated_timestamp']))
        self.assertAlmostEqual(row['raw_left_unextrapolated_position_x'],-.45)
        window=telemetry.extract_window(decoded,7)
        self.assertEqual(window['schemaVersion'],5)
        self.assertEqual(window['header']['input'],'Meta hand tracking')
        self.assertEqual(window['frames'][0]['hand_wmm2_enabled'],1)
        self.assertEqual(window['frames'][0]['hand_fmm_requested'],0)
    def test_cadence_does_not_bridge_estimated_or_low_confidence_samples(self):
        rows=[{'timestamp':i*.1,'raw_left_tracked':1,'raw_head_tracked':1,'raw_body_tracked':1,
               'raw_left_velocity_y':velocity} for i,velocity in enumerate((1,-1,0,0,1,-1))]
        # Missing provenance is historical controller data, retaining cadence behavior.
        self.assertAlmostEqual(telemetry.stroke_frequencies(rows,'left')[0],2.5)
        for source in (2,3,4):
            rows[3]['raw_left_pose_source']=source
            self.assertEqual(telemetry.stroke_frequencies(rows,'left'),[])
        rows[3]['raw_left_pose_source']=1
        rows[3]['raw_left_motion_estimated']=1
        self.assertEqual(telemetry.stroke_frequencies(rows,'left'),[])
        rows[3]['raw_left_motion_estimated']=0
        self.assertAlmostEqual(telemetry.stroke_frequencies(rows,'left')[0],2.5)
    def test_partial_tail_recovers_finished_frames(self):
        d=self.decode(self.fixture(b'\x01\x10'))
        self.assertTrue(d['truncated']);self.assertFalse(d['complete']);self.assertEqual(len(d['frames']),1)
    def test_reject_version_and_size(self):
        with self.assertRaises(ValueError):self.decode(self.fixture(version=99))
        with self.assertRaises(ValueError):self.decode(self.fixture(struct.pack('<Bi',1,-1)))
    def test_percentiles_and_unavailable(self):
        self.assertEqual(telemetry.percentile([1,2,3],.5),2)
        self.assertIsNone(telemetry.stats([float('nan')])['p95'])
    def test_wrist_excursion_excludes_rigid_torso_yaw(self):
        r={}
        for prefix,q in [('raw_left_rotation',[0,2**-.5,0,2**-.5]),('raw_body_rotation',[0,2**-.5,0,2**-.5]),('calibration_left_rotation',[0,0,0,1]),('calibration_heading',[0,0,0,1])]:
            r.update({prefix+'_'+c:v for c,v in zip('xyzw',q)})
        self.assertAlmostEqual(telemetry.wrist_excursion(r,'left'),0)
    def test_body_translation_and_yaw(self):
        r={f'raw_left_position_{c}':v for c,v in zip('xyz',[10,2,1])}
        r.update({f'raw_head_position_{c}':v for c,v in zip('xyz',[10,2,0])})
        r.update({f'raw_body_rotation_{c}':v for c,v in zip('xyzw',[0,2**-.5,0,2**-.5])})
        v=telemetry.body_relative(r,'left');self.assertAlmostEqual(v[0],-1);self.assertAlmostEqual(v[2],0)

if __name__=='__main__':unittest.main()
