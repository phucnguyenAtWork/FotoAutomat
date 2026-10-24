import unittest
import numpy as np
from RegionQuality import assess,route_mask,apply_regional,iou,reject_overlapping_faces
class Tests(unittest.TestCase):
 def test_duplicate_faces_rejected(self):
  mask=np.ones((20,20),bool);q=[{'eligible':True,'reasons':[]} for _ in range(2)]
  self.assertEqual(len(reject_overlapping_faces([mask,mask],q)),1)
  self.assertTrue(all(not x['eligible'] for x in q))
 def test_small_patch_rejected(self):
  g=np.ones((40,40),bool);c=np.zeros_like(g);c[:8,:8]=True
  m,q=assess(g,c,np.ones(g.shape));self.assertFalse(q['eligible']);self.assertGreater(q['accepted_pixels'],50)
 def test_full_and_confidence_loss(self):
  g=np.ones((40,40),bool);_,q=assess(g,g,np.ones(g.shape));self.assertTrue(q['eligible'])
  _,q=assess(g,g,np.full(g.shape,.6));self.assertEqual(q['diagnostic_stage'],'confidence_filter_loss');self.assertFalse(q['eligible'])
 def test_empty(self):
  g=np.zeros((10,10),bool);m,q=assess(g,g,np.zeros(g.shape));self.assertFalse(q['eligible']);self.assertIsNone(iou(m,m))
 def test_fallback_preserves_pixels(self):
  x=np.zeros((20,20,3),np.uint8);y=np.full_like(x,255);m=np.ones((20,20),bool)
  for q,stable in [({'eligible':False},True),({'eligible':True},False)]:
   z,decision=apply_regional(x,y,m,q,stable);np.testing.assert_array_equal(z,x);self.assertIn('skip',decision)
  z,_=apply_regional(x,y,m,{'eligible':True});np.testing.assert_array_equal(z,y)
 def test_localized_zones(self):
  g=np.ones((40,40),bool);c=g.copy();c[20:]=False
  _,q=assess(g,c,np.ones(g.shape));self.assertIn('localized_or_missing_zones',q['reasons'])
if __name__=='__main__':unittest.main()
