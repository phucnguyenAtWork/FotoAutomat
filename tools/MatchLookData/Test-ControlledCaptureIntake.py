import copy,hashlib,tempfile,unittest
from pathlib import Path
from PIL import Image
from ControlledCaptureIntake import audit,CONFIG_KEYS
class Tests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
  cfg={k:'recorded' for k in CONFIG_KEYS};cfg.update(id='cfg',exposure_mode='M',auto_iso=False,iso=100,shutter_seconds=.005,aperture=8,wb_mode='custom',flash_mode='manual',subject_positions=['center'])
  self.data=dict(schema='fotoautomat-controlled-capture-v1',configs=[cfg],captures=[])
 def add(self,partition='calibration',n=1):
  cid=f'{partition}-{n}';record=dict(id=cid,config_id='cfg',session_id=partition,burst_id=cid,partition=partition,subject='chart',position_id='center',rights_confirmed=True,continuous_on_during_capture=True,pairing_verified=True)
  for role,ext in (('raw','RAF'),('jpeg','JPG'),('liveview','PNG')):
   p=self.root/f'{cid}-{role}.{ext}'
   if role=='raw':p.write_bytes(b'not a real RAW fixture '+cid.encode())
   else:Image.new('RGB',(8,8),(n%255,20,30)).save(p)
   record[role]=dict(path=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
  self.data['captures'].append(record);return record
 def test_empty_is_not_ready(self):
  r=audit(self.data,self.root);self.assertFalse(r['intake_complete']);self.assertIn('no_captures',r['blockers']);self.assertFalse(r['training_ready'])
 def test_hash_detects_changes(self):
  c=self.add();(self.root/c['raw']['path']).write_bytes(b'changed');self.assertTrue(any('hash_mismatch' in e for e in audit(self.data,self.root)['errors']))
 def test_no_root_escape(self):
  c=self.add();c['jpeg']['path']='../outside.jpg';self.assertTrue(any('path_outside_root' in e for e in audit(self.data,self.root)['errors']))
 def test_pairing_is_not_assumed(self):
  c=self.add();c['pairing_verified']=False;self.assertTrue(any('pairing_pending' in e for e in audit(self.data,self.root)['blockers']))
 def test_group_overlap(self):
  a=self.add();b=self.add('validation');b['session_id']=a['session_id'];self.assertTrue(any('cross_partition' in e for e in audit(self.data,self.root)['errors']))
 def test_exact_hash_overlap(self):
  a=self.add();b=self.add('validation');b['jpeg']=a['jpeg'];self.assertIn('identical_file_cross_partition',audit(self.data,self.root)['errors'])
 def test_corrupt_image(self):
  c=self.add();p=self.root/c['jpeg']['path'];p.write_bytes(b'not jpeg');c['jpeg']['sha256']=hashlib.sha256(p.read_bytes()).hexdigest();self.assertTrue(any('decode_error' in e for e in audit(self.data,self.root)['errors']))
 def test_auto_settings_block(self):
  self.add();self.data['configs'][0]['auto_iso']=True;self.assertTrue(any('not_confirmed_locked' in e for e in audit(self.data,self.root)['blockers']))
 def test_file_presence_never_proves_raw_decode_or_profile(self):
  self.add();r=audit(self.data,self.root);self.assertTrue(r['structure_valid']);self.assertFalse(r['profile_ready']);self.assertFalse(next(f for f in r['files'] if f['role']=='raw')['decode_verified'])
 def test_complete_intake_is_still_not_training_or_profile_approval(self):
  for part,offset in (('calibration',0),('validation',100)):
   n=offset
   for subject in ('chart','one_person','four_people'):
    for repeat in range(3):
     n+=1;c=self.add(part,n);c['subject']=subject
  r=audit(self.data,self.root);self.assertTrue(r['intake_complete'],r);self.assertFalse(r['training_ready']);self.assertFalse(r['profile_ready'])
if __name__=='__main__':unittest.main()
