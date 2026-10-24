"""Match Look parameter predictor (P2, protocol section 13): data sampling, model, losses, fixed evaluation sets.
Training only; the app will run the exported ONNX model and decode with RecipeVector (C#)."""
import json, random
from pathlib import Path
import torch, torch.nn as nn, torch.nn.functional as F
import importlib.util
_s = importlib.util.spec_from_file_location('crt', Path(__file__).with_name('ColorRenderTorch.py')); crt = importlib.util.module_from_spec(_s); _s.loader.exec_module(crt)

TRAIN_SIZE = 128

# ---------- data ----------
class Content:
    """All content images in memory: rgb256[i] uint8 [3,256,256]; kind/split per item."""
    def __init__(self, root: Path):
        m = json.loads((root / 'content-manifest.json').read_text(encoding='utf-8'))['items']
        self.items = m; self.index = {r['id']: i for i, r in enumerate(m)}
        raw = [torch.frombuffer(bytearray((root / 'content' / (r['id'] + '.bgra')).read_bytes()), dtype=torch.uint8).reshape(256, 256, 4) for r in m]
        self.rgb256 = torch.stack([t[..., [2, 1, 0]].permute(2, 0, 1).contiguous() for t in raw])
        self.rgb128 = (F.avg_pool2d(self.rgb256.float(), 2).round()).to(torch.uint8)
    def pool(self, split, kind=None): return [i for i, r in enumerate(self.items) if r['split'] == split and (kind is None or r['kind'] == kind)]

def load_recipes(path: Path):
    rows = [json.loads(l) for l in path.read_text().splitlines() if l.strip()]
    return {s: torch.tensor([r['unit'] for r in rows if r['split'] == s], dtype=torch.float32) for s in ('train', 'validation', 'test')}

def sample_pairs(content: Content, split: str, n: int, rng: random.Random, portrait_a=.7):
    portraits, scenes = content.pool(split, 'portrait'), content.pool(split, 'scene')
    pairs = []
    for _ in range(n):
        a = rng.choice(portraits if rng.random() < portrait_a or not scenes else scenes)
        while True:
            b = rng.choice(portraits if rng.random() < .5 or not scenes else scenes)
            if b != a: break
        pairs.append((a, b))
    return pairs

def fixed_set(content, recipes, split, n, seed, scene_identity=0):
    """Deterministic evaluation triplets (A index, B index, unit). scene_identity: extra pairs with B = scene, R = identity."""
    rng = random.Random(seed); units = recipes[split]
    rows = [(a, b, units[rng.randrange(len(units))]) for a, b in sample_pairs(content, split, n, rng)]
    scenes, portraits = content.pool(split, 'scene'), content.pool(split, 'portrait')
    rows += [(rng.choice(portraits), rng.choice(scenes), torch.zeros(units.shape[1])) for _ in range(scene_identity)]
    return rows

# ---------- colour ----------
def srgb_to_lab(rgb):
    lin = torch.where(rgb <= .04045, rgb / 12.92, ((rgb.clamp_min(0) + .055) / 1.055) ** 2.4)
    m = rgb.new_tensor([[.4124564, .3575761, .1804375], [.2126729, .7151522, .0721750], [.0193339, .1191920, .9503041]])
    xyz = torch.einsum('ij,bjhw->bihw', m, lin) / rgb.new_tensor([.95047, 1., 1.08883])[None, :, None, None]
    f = torch.where(xyz > (6 / 29) ** 3, xyz.clamp_min(1e-12) ** (1 / 3), xyz / (3 * (6 / 29) ** 2) + 4 / 29)
    return torch.stack([116 * f[:, 1] - 16, 500 * (f[:, 0] - f[:, 1]), 200 * (f[:, 1] - f[:, 2])], 1)

def skin_mask(rgb):
    """Same colour key as ReferenceMatchEngine.IsSkinColor (BT.601 YCbCr + chroma >= 30), on 0..1 RGB."""
    r, g, b = (rgb[:, i] * 255 for i in range(3))
    y = .299 * r + .587 * g + .114 * b; cb = 128 - .168736 * r - .331264 * g + .5 * b; cr = 128 + .5 * r - .418688 * g - .081312 * b
    chroma = torch.maximum(r, torch.maximum(g, b)) - torch.minimum(r, torch.minimum(g, b))
    return ((chroma >= 30) & (y >= 30) & (y <= 245) & (cb >= 77) & (cb <= 127) & (cr >= 133) & (cr <= 173)).float()

# ---------- model ----------
class Encoder(nn.Module):
    def __init__(self):
        super().__init__()
        ch = [3, 16, 32, 64, 96, 128]
        self.convs = nn.ModuleList(nn.Conv2d(ch[i], ch[i + 1], 3, stride=2, padding=1) for i in range(5))
    def forward(self, x):
        for c in self.convs: x = F.relu(c(x))
        # global statistics: what colours, how spread (population std + eps: defined for 1x1 maps, smooth gradient)
        return torch.cat([x.mean((2, 3)), (x.var((2, 3), unbiased=False) + 1e-6).sqrt()], 1)

class ParamPredictor(nn.Module):
    def __init__(self, n_params=52):
        super().__init__(); self.encoder = Encoder()
        self.head = nn.Sequential(nn.Linear(768, 256), nn.ReLU(), nn.Linear(256, n_params))
    def forward(self, reference, target):
        zr, zt = self.encoder(reference), self.encoder(target)
        return torch.tanh(self.head(torch.cat([zr, zt, zr - zt], 1)))

def image_loss(pred_rgb, gt_rgb, source_rgb, skin_weight=3.):
    de = (srgb_to_lab(pred_rgb) - srgb_to_lab(gt_rgb)).norm(dim=1)
    w = 1 + (skin_weight - 1) * skin_mask(source_rgb)
    return (de * w).sum() / w.sum() / 10
