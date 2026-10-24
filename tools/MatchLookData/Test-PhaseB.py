import copy
import unittest
import numpy as np
from PhaseBData import LOOKS, render, validate_dataset

class PhaseBTests(unittest.TestCase):
    def test_neutral_preserves_encoded_pixels(self):
        image=np.random.default_rng(7).integers(0,256,(17,23,3),dtype=np.uint8)
        np.testing.assert_array_equal(render(image,LOOKS[0]),image)

    def test_monochrome_is_achromatic_and_looks_bounded(self):
        image=np.random.default_rng(8).integers(0,256,(17,23,3),dtype=np.uint8)
        for look in LOOKS:
            out=render(image,look)
            self.assertEqual(out.shape,image.shape); self.assertEqual(out.dtype,np.uint8)
        mono=render(image,LOOKS[-1])
        np.testing.assert_array_equal(mono[:,:,0],mono[:,:,1])
        np.testing.assert_array_equal(mono[:,:,0],mono[:,:,2])

    def fixture(self):
        return dict(assets=[dict(id=i,split='diagnostic_only',leakage_group='same',
            group_review='pending',source_role_review='pending',rights_status='user_confirmed') for i in ('a','b')],
            triplets=[dict(id='t',source_id='a',reference_source_id='b',split='diagnostic_only',review_status='pending')],holdout_locked=False)

    def test_pending_never_ready(self):
        result=validate_dataset(self.fixture())
        self.assertTrue(result['structure_valid']); self.assertFalse(result['training_ready'])
        self.assertIn('target_look_review_pending',result['blockers'])

    def test_reject_leakage_and_missing_parent(self):
        data=self.fixture(); data['assets'][1]['split']='test'
        self.assertIn('leakage_group_crosses_splits',validate_dataset(data)['errors'])
        self.assertIn('cross_split_triplet',validate_dataset(data)['errors'])
        data=self.fixture(); data['triplets'][0]['reference_source_id']='absent'
        self.assertIn('missing_parent',validate_dataset(data)['errors'])

if __name__=='__main__': unittest.main()
