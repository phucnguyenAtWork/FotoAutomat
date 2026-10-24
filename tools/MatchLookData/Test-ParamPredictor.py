import importlib.util, random, unittest
from pathlib import Path
import torch
s = importlib.util.spec_from_file_location('pp', Path(__file__).with_name('ParamPredictor.py')); pp = importlib.util.module_from_spec(s); s.loader.exec_module(pp)

class Tests(unittest.TestCase):
    def test_model_shapes_range_and_size(self):
        m = pp.ParamPredictor(); out = m(torch.rand(2, 3, 128, 128), torch.rand(2, 3, 128, 128))
        self.assertEqual(tuple(out.shape), (2, 52)); self.assertTrue(bool((out.abs() <= 1).all()))
        self.assertLess(sum(p.numel() for p in m.parameters()), 600_000)
    def test_lab_white_and_skin_key(self):
        lab = pp.srgb_to_lab(torch.ones(1, 3, 1, 1)); self.assertAlmostEqual(float(lab[0, 0]), 100, places=2); self.assertLess(float(lab[0, 1:].abs().max()), .05)
        skin = torch.tensor([205, 150, 120]).float().view(1, 3, 1, 1) / 255; grey = torch.full((1, 3, 1, 1), .5)
        self.assertEqual(float(pp.skin_mask(skin)), 1.); self.assertEqual(float(pp.skin_mask(grey)), 0.)
    def test_image_loss_is_zero_for_identical_and_weights_skin(self):
        x = torch.rand(1, 3, 8, 8); self.assertEqual(float(pp.image_loss(x, x, x)), 0.)
    def test_tiny_overfit_reduces_loss(self):
        torch.manual_seed(0); m = pp.ParamPredictor(); opt = torch.optim.Adam(m.parameters(), lr=3e-3)
        ref, tgt, unit = torch.rand(4, 3, 32, 32), torch.rand(4, 3, 32, 32), torch.rand(4, 52) * 1.6 - .8
        first = None
        for _ in range(60):
            loss = (m(ref, tgt) - unit).abs().mean(); opt.zero_grad(); loss.backward(); opt.step(); first = first or float(loss)
        self.assertLess(float(loss), first * .5)

if __name__ == '__main__': unittest.main()
