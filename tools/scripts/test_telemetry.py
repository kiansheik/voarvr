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
