"""Gate for P1: the PyTorch renderer must reproduce C# ColorProcessor renders (max 1/255) before it is used in training.
Run: python Test-ColorRenderParity.py <param-dataset dir>   (needs param-schema.json, content-set/, parity-csharp/)"""
import importlib.util, json, sys, unittest
from pathlib import Path
import torch
s = importlib.util.spec_from_file_location('crt', Path(__file__).with_name('ColorRenderTorch.py')); crt = importlib.util.module_from_spec(s); s.loader.exec_module(crt)
DATA = Path(sys.argv.pop(1)) if len(sys.argv) > 1 and not sys.argv[1].startswith('-') else None

class Parity(unittest.TestCase):
    @unittest.skipIf(DATA is None, 'dataset folder not given')
    def test_matches_csharp_renders(self):
        names, lo, hi = crt.load_schema(DATA / 'param-schema.json'); pairs = json.loads((DATA / 'parity-csharp/pairs.json').read_text())
        worst, total, count, bad = 0, 0., 0, 0
        for p in pairs:
            rgb = crt.bgra_to_rgb((DATA / 'content-set/content' / (p['content'] + '.bgra')).read_bytes(), p['size'])[None]
            raw = crt.unit_to_raw(torch.tensor([p['unit']], dtype=torch.float64), lo, hi)
            got = (crt.render(rgb, raw) * 255).round()[0]
            want = crt.bgra_to_rgb((DATA / 'parity-csharp' / f"{p['k']}.out.bgra").read_bytes(), p['size']) * 255
            d = (got - want).abs(); worst = max(worst, float(d.max())); total += float(d.sum()); count += d.numel(); bad += int((d > 1).sum())
        print(f'\n{len(pairs)} pairs: max |diff| {worst:.0f}/255, mean {total / count:.5f}/255, values off by >1: {bad}')
        self.assertLessEqual(worst, 1)

class Gradients(unittest.TestCase):
    def test_every_parameter_gets_a_finite_gradient(self):
        torch.manual_seed(0); rgb = torch.rand(2, 3, 16, 16, dtype=torch.float64)
        lo = torch.tensor([-1.5, -60, -40, -50, -60, -60, -50, -50, -60, -50] + [-.15] * 12 + [-30, -60, -40] * 8 + [-40] * 6, dtype=torch.float64); hi = -lo
        unit = (torch.rand(2, 52, dtype=torch.float64) * 1.2 - .6).requires_grad_()
        out = crt.render(rgb, crt.unit_to_raw(unit, lo, hi), quantize='ste'); (out * torch.rand_like(out)).sum().backward()
        self.assertTrue(torch.isfinite(unit.grad).all()); self.assertGreater(int((unit.grad.abs() > 0).sum()), 80)
    def test_identity_recipe_is_identity(self):
        rgb = (torch.randint(0, 256, (1, 3, 8, 8)).double() / 255)
        self.assertTrue(torch.equal(crt.render(rgb, torch.zeros(1, 52, dtype=torch.float64)), rgb))

if __name__ == '__main__': unittest.main()
