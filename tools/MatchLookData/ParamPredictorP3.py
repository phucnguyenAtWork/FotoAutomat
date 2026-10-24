"""P3 parameter predictor (protocol sections 16-17): Deep Preset-style Encoder T on [reference, target], K references,
52 bounded parameters, balance and image-safety guardrails. Training only; the app decodes outputs with RecipeVector."""
import json, math, random
from pathlib import Path
import torch, torch.nn as nn, torch.nn.functional as F
import importlib.util
_s = importlib.util.spec_from_file_location('pp', Path(__file__).with_name('ParamPredictor.py')); pp = importlib.util.module_from_spec(_s); _s.loader.exec_module(pp)
_r = importlib.util.spec_from_file_location('rl', Path(__file__).with_name('Render-LookLibrary.py')); rl = importlib.util.module_from_spec(_r); _r.loader.exec_module(rl)
crt = pp.crt
GROUP_WEIGHTS = [(0, 3, 1.), (3, 8, 1.), (8, 10, 1.5), (10, 22, 2.), (22, 46, 2.), (46, 52, 2.)]

# ---------- looks ----------
def load_looks(library: Path, splits: Path, names, lo, hi):
    """id -> unit vector of each library look, and split -> list of ids."""
    looks = json.loads(library.read_text(encoding='utf-8'))['looks']; sp = json.loads(splits.read_text(encoding='utf-8'))
    unit = {l['id']: (2 * (rl.to_raw(l['params'], names, lo, hi) - lo) / (hi - lo) - 1).float() for l in looks}
    held = set(sp['test']) | set(sp['validation'])
    return unit, dict(train=[k for k in unit if k not in held], validation=list(sp['validation']), test=list(sp['test']))

def look_sample(unit_by_id, ids, rng: random.Random, gen: torch.Generator):
    """A library look at strength U(0.6, 1.2) with N(0, 0.05) jitter on the parameters it uses."""
    u = unit_by_id[rng.choice(ids)]; active = (u != 0).float()
    return (u * (0.6 + 0.6 * rng.random()) + active * torch.randn(u.shape, generator=gen) * 0.05).clamp(-1, 1)

def control_groups(n_params=52):
    """Parameters that form one control: each tone slider and HSL field alone, a curve's 3 points, a wheel's X/Y."""
    g = list(range(22 - 12)) + [10 + i // 3 for i in range(12)] + [14 + i for i in range(24)] + [38 + i // 2 for i in range(6)]
    return torch.tensor(g[:n_params])

def look_augment(unit_by_id, ids, rng: random.Random, gen: torch.Generator, groups=None):
    """Augmented look (protocol section 17, option b): 1 look (50%), a blend of 2 (30%) or 3 (20%) library looks with
    Dirichlet(1) weights; each active control dropped with p 0.2 (never all); strength U(0.3, 1.3); N(0, 0.05) jitter.
    Returns (unit, ids used)."""
    groups = control_groups() if groups is None else groups
    n = 1 if (r := rng.random()) < .5 else 2 if r < .8 else 3; used = rng.sample(ids, n)
    w = [rng.gammavariate(1, 1) for _ in used]
    u = sum(wi / sum(w) * unit_by_id[i] for wi, i in zip(w, used))
    present = sorted({int(g) for g in groups[u != 0]})
    keep = [g for g in present if rng.random() >= .2] or [rng.choice(present)] if present else []
    u = u * torch.isin(groups, torch.tensor(keep, dtype=groups.dtype)).float(); active = (u != 0).float()
    return (u * (0.3 + rng.random()) + active * torch.randn(u.shape, generator=gen) * 0.05).clamp(-1, 1), used

# ---------- model ----------
class EncoderT(nn.Module):
    def __init__(self):
        super().__init__(); ch = [6, 16, 32, 64, 96, 128, 160]
        self.convs = nn.ModuleList(nn.Conv2d(ch[i], ch[i + 1], 3, stride=2, padding=1) for i in range(6))
    def forward(self, reference, target):
        x = torch.cat([reference, target], 1)
        for c in self.convs: x = F.leaky_relu(c(x), .1)
        return torch.cat([x.mean((2, 3)), (x.var((2, 3), unbiased=False) + 1e-6).sqrt()], 1)

class LookPredictor(nn.Module):
    def __init__(self, n_params=52):
        super().__init__(); self.t = EncoderT()
        self.embed = nn.Sequential(nn.Linear(320, 256), nn.LeakyReLU(.1))
        self.head = nn.Sequential(nn.Linear(256, 256), nn.LeakyReLU(.1), nn.Linear(256, n_params))
    def look_vector(self, references, target):
        """references: list of K tensors [B,3,H,W]; returns the look embedding averaged over references."""
        return self.embed(torch.stack([self.t(r, target) for r in references]).mean(0))
    def forward(self, references, target):
        z = self.look_vector(references, target); return torch.tanh(self.head(z)), z

# ---------- guardrail losses ----------
def economy(unit):
    return sum(w * unit[:, a:b].abs().mean(1) for a, b, w in GROUP_WEIGHTS).mean() / len(GROUP_WEIGHTS)

def conflicts(raw):
    """Warmth against the warm axis (hue 40°) of the Color Balance wheels; Exposure against Whites. (Exposure up with
    Highlights down is a legitimate recovery move used by several approved looks, so it is not penalised.)"""
    x, y = raw[:, 46:52:2], raw[:, 47:52:2]; warm_axis = (x * math.cos(math.radians(40)) + y * math.sin(math.radians(40))).sum(1) / 40
    warmth = raw[:, 1] / 60; exposure = raw[:, 0] / 1.5; whites = raw[:, 6] / 50
    return (F.relu(-warmth * warm_axis) + F.relu(-exposure * whites)).mean()

def skin_guard(pred, gt, source):
    skin = pp.skin_mask(source); n = skin.sum().clamp_min(1)
    lp, lg = pp.srgb_to_lab(pred)[:, 0], pp.srgb_to_lab(gt)[:, 0]
    l_err = ((lp - lg).abs() * skin).sum() / n / 10  # skin mask is [B,H,W]
    # same soft threshold on both sides: a prediction equal to the truth is never penalised
    clip = (F.relu(torch.sigmoid((lp - 97) * 2) - torch.sigmoid((lg - 97) * 2)) * skin).sum() / n
    return l_err, clip
