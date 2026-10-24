import importlib.util
import unittest
from pathlib import Path
from PIL import Image

spec=importlib.util.spec_from_file_location('review',Path(__file__).with_name('Preprocess-Review.py'))
m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m)

class MetricsTests(unittest.TestCase):
    def test_black_white(self):
        black=m.measure(Image.new('RGB',(32,24),(0,0,0)))
        white=m.measure(Image.new('RGB',(32,24),(255,255,255)))
        self.assertEqual(black['near_black_pixel_pct'],100)
        self.assertEqual(white['near_white_pixel_pct'],100)
        self.assertEqual(black['channel_high_pct'],0)
        self.assertEqual(black['laplacian_variance_proxy'],0)
    def test_red_not_white(self):
        red=m.measure(Image.new('RGB',(32,24),(255,0,0)))
        self.assertEqual(red['near_white_pixel_pct'],0)
        self.assertAlmostEqual(red['channel_high_pct'],100/3,places=4)
        self.assertAlmostEqual(red['encoded_luma_p50'],.2126,places=4)
    def test_unique_schema(self):
        self.assertEqual(len(set(m.AUTO+m.MANUAL)),40)

if __name__=='__main__': unittest.main()
