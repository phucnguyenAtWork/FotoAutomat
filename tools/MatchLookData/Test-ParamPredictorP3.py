import importlib.util, random, unittest
from pathlib import Path
import torch
s = importlib.util.spec_from_file_location('p3', Path(__file__).with_name('ParamPredictorP3.py')); p3 = importlib.util.module_from_spec(s); s.loader.exec_module(p3)
NAMES = [p for p in ['Exposure', 'Warmth', 'Tint', 'Contrast', 'Highlights', 'Shadows', 'Whites', 'Blacks', 'Saturation', 'Vibrance']]
NAMES += [f'Curve.{c}.{k}' for c in ('RGB', 'Red', 'Green', 'Blue') for k in ('0.25', '0.50', '0.75')]
NAMES += [f'HSL.{b}.{f}' for b in ('Red', 'Orange', 'Yellow', 'Green', 'Aqua', 'Blue', 'Purple', 'Magenta') for f in ('Hue', 'Saturation', 'Lightness')]
NAMES += [f'Balance.{w}.{a}' for w in ('Shadows', 'Midtones', 'Highlights') for a in ('X', 'Y')]
HI = torch.tensor([1.5, 60, 40, 50, 60, 60, 50, 50, 60, 50] + [.15] * 12 + [30, 60, 40] * 8 + [40] * 6, dtype=torch.float64); LO = -HI
HERE = Path(__file__).parent

class Tests(unittest.TestCase):
    def setUp(self): self.unit, self.splits = p3.load_looks(HERE / 'look-library-v1.json', HERE / 'look-splits-p3.json', NAMES, LO, HI)
    def test_splits_are_disjoint_and_complete(self):
        all_ids = set(self.unit); parts = [set(v) for v in self.splits.values()]
        self.assertEqual(set().union(*parts), all_ids); self.assertEqual(sum(map(len, parts)), len(all_ids)); self.assertEqual(len(self.splits['train']), 22)
    def test_conflict_guardrail_does_not_penalise_any_approved_look(self):
        for k, u in self.unit.items():
            raw = p3.crt.unit_to_raw(u[None].double(), LO, HI); self.assertEqual(float(p3.conflicts(raw)), 0., k)
    def test_conflict_guardrail_fires_on_opposed_controls(self):
        raw = torch.zeros(1, 52, dtype=torch.float64); raw[0, 1] = 40; raw[0, 46], raw[0, 47] = -30, -20   # warm Warmth, cool shadows wheel
        self.assertGreater(float(p3.conflicts(raw)), 0)
    def test_look_sample_bounded_and_model_shapes(self):
        rng, gen = random.Random(1), torch.Generator().manual_seed(1)
        u = p3.look_sample(self.unit, self.splits['train'], rng, gen); self.assertTrue(bool((u.abs() <= 1).all()))
        m = p3.LookPredictor(); refs = [torch.rand(2, 3, 64, 64) for _ in range(3)]; out, z = m(refs, torch.rand(2, 3, 64, 64))
        self.assertEqual(tuple(out.shape), (2, 52)); self.assertEqual(tuple(z.shape), (2, 256)); self.assertTrue(bool((out.abs() <= 1).all()))
    def test_look_augment_bounded_uses_library_looks_and_keeps_a_control(self):
        rng, gen = random.Random(2), torch.Generator().manual_seed(2); sizes = set()
        for _ in range(300):
            u, used = p3.look_augment(self.unit, self.splits['train'], rng, gen); sizes.add(len(used))
            self.assertTrue(bool((u.abs() <= 1).all())); self.assertTrue(set(used) <= set(self.splits['train'])); self.assertTrue(bool((u != 0).any()))
        self.assertEqual(sizes, {1, 2, 3})
    def test_control_groups_pair_curves_and_wheels(self):
        g = p3.control_groups(); self.assertEqual(len(g), 52); self.assertEqual(len(set(g[10:13].tolist())), 1); self.assertEqual(int(g[46]), int(g[47])); self.assertNotEqual(int(g[47]), int(g[48]))
    def test_economy_and_skin_guard_zero_at_rest(self):
        self.assertEqual(float(p3.economy(torch.zeros(3, 52))), 0.); x = torch.rand(1, 3, 16, 16)
        l, c = p3.skin_guard(x, x, x); self.assertEqual(float(l), 0.); self.assertEqual(float(c), 0.)

if __name__ == '__main__': unittest.main()
