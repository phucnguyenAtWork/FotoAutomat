import hashlib,json,unittest
from TrainingReview import validate_review
class Tests(unittest.TestCase):
 def setUp(self):
  self.d={'assets':[{'id':'a'}]};self.raw=json.dumps(self.d).encode();self.r={'dataset_sha256':hashlib.sha256(self.raw).hexdigest(),'assets':[dict(id='a',decision='reviewed',base_look='presetA',subject_groups='P01',duplicate_group='unique',domain='portrait')]}
 def test_labels_never_authorize(self):
  result=validate_review(self.r,self.d,self.raw);self.assertTrue(result['labels_complete']);self.assertFalse(result['training_allowed'])
 def test_unknown_stays_pending(self):
  self.r['assets'][0]['base_look']='unknown';self.assertFalse(validate_review(self.r,self.d,self.raw)['labels_complete'])
 def test_hash_rejects(self):
  self.r['dataset_sha256']='changed';self.assertIn('dataset_hash_mismatch',validate_review(self.r,self.d,self.raw)['errors'])
 def test_duplicate_rejects(self):
  self.r['assets']*=2;self.assertIn('asset_set_mismatch',validate_review(self.r,self.d,self.raw)['errors'])
if __name__=='__main__':unittest.main()
