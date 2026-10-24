import unittest,importlib.util
from pathlib import Path
import numpy as np
spec=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('Audit-PortraitRegions.py'));a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
class Tests(unittest.TestCase):
 def test_empty(self):self.assertEqual(a.metrics(np.zeros((2,2,3),np.uint8),np.zeros((2,2,3),np.uint8),np.zeros((2,2),bool))['pixels'],0)
 def test_identity(self):
  x=np.full((3,3,3),100,np.uint8);r=a.metrics(x,x,np.ones((3,3),bool));self.assertEqual(r['deltaE76_mean'],0);self.assertEqual(r['new_near_ceiling_vs_target'],0)
 def test_new_clipping_and_roi(self):
  x=np.zeros((2,2,3),np.uint8);y=x.copy();x[0,0]=255;m=np.zeros((2,2),bool);m[0,0]=True
  self.assertEqual(a.metrics(x,y,m)['new_near_ceiling_vs_target'],1);self.assertEqual(a.metrics(x,y,~m)['mae_255'],0)
if __name__=='__main__':unittest.main()
