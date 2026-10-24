import importlib.util, tempfile, unittest
from pathlib import Path
from PIL import Image
s = importlib.util.spec_from_file_location('ppc', Path(__file__).with_name('Prepare-ParamContent.py')); ppc = importlib.util.module_from_spec(s); s.loader.exec_module(ppc)

class Tests(unittest.TestCase):
    def test_split_is_deterministic_per_group(self):
        self.assertEqual(ppc.split_of('booth:a'), ppc.split_of('booth:a'))
        self.assertTrue({ppc.split_of(f'g{i}') for i in range(200)} == {'train', 'validation', 'test'})
    def test_bgra_layout_crop_and_exif(self):
        with tempfile.TemporaryDirectory() as d:
            p = Path(d) / 'x.png'; im = Image.new('RGB', (600, 300), (200, 100, 50)); im.save(p)
            raw = ppc.to_bgra(p); self.assertEqual(len(raw), ppc.SIZE * ppc.SIZE * 4)
            self.assertEqual(tuple(raw[:4]), (50, 100, 200, 255))  # B, G, R, A
            q = Path(d) / 'r.jpg'; im2 = Image.new('RGB', (300, 600), (10, 20, 30)); exif = im2.getexif(); exif[0x0112] = 6; im2.save(q, exif=exif)
            self.assertEqual(len(ppc.to_bgra(q)), ppc.SIZE * ppc.SIZE * 4)

if __name__ == '__main__': unittest.main()
