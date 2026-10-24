"""Differentiable PyTorch port of FotoAutomat's ColorProcessor, restricted to the 52 RecipeVector parameters.

TRAINING ONLY. The C# ColorProcessor (src/FotoAutomat.Core) is the source of truth for the app and for evaluation; this
port must pass Test-ColorRenderParity.py (max 1/255 against C# renders) before it is used. It mirrors, for a recipe
produced by RecipeVector.Decode (no LUT, spatial tools, layers, Monochrome, Levels, Calibration or Kelvin):
  stage 1 ColorProcessor.Apply: per-channel exposure/white-balance gain in linear light, contrast in sRGB, tone shifts
          (Highlights, Shadows, Whites, Blacks), saturation and vibrance -> rounded to bytes;
  stage 2 AdvancedColorProcessor.Apply: RGB curve then channel curves, 8-band HSL, Color Balance wheels -> bytes.
Rounding is round-half-to-even like C# Math.Round. quantize='ste' keeps gradients through the rounding (straight-through).
"""
import json, math
from pathlib import Path
import torch

KNOTS = (.25, .5, .75)
HUE_CENTERS = (0., 30., 60., 120., 180., 240., 270., 300.)

def load_schema(path):
    params = json.loads(Path(path).read_text(encoding='utf-8'))['parameters']
    lo = torch.tensor([p['Min'] for p in params], dtype=torch.float64); hi = torch.tensor([p['Max'] for p in params], dtype=torch.float64)
    return [p['Name'] for p in params], lo, hi

def unit_to_raw(unit, lo, hi):
    """RecipeVector.FromUnit then Decode clamping: raw values in the schema ranges."""
    lo, hi = lo.to(unit), hi.to(unit)
    return (hi + lo) / 2 + unit.clamp(-1, 1) * (hi - lo) / 2

def _q(x, mode):
    y = (x * 255).round().clamp(0, 255) / 255
    return y if mode == 'round' else x + (y - x).detach() if mode == 'ste' else x

def _smooth(lo, hi, v):
    t = ((v - lo) / (hi - lo)).clamp(0, 1); return t * t * (3 - 2 * t)

def _srgb_to_linear(v): return torch.where(v <= .04045, v / 12.92, ((v.clamp_min(0) + .055) / 1.055) ** 2.4)
def _linear_to_srgb(v): return torch.where(v <= .0031308, 12.92 * v, 1.055 * v.clamp_min(1e-12) ** (1 / 2.4) - .055)

def _curve(x, ys):
    """ToneCurve.Evaluate for points (0,0),(.25,y1),(.5,y2),(.75,y3),(1,1); ys: [B,3] broadcast over x [B,H,W]."""
    xs = (0., .25, .5, .75, 1.); y = [torch.zeros_like(ys[:, 0]), ys[:, 0], ys[:, 1], ys[:, 2], torch.ones_like(ys[:, 0])]
    x = x.clamp(0, 1); out = torch.zeros_like(x)
    for i in range(1, 5):
        a, b = y[i - 1][:, None, None], y[i][:, None, None]
        seg = a + (b - a) * (x - xs[i - 1]) / (xs[i] - xs[i - 1])
        inside = (x <= xs[i]) & ((x > xs[i - 1]) if i > 1 else torch.ones_like(x, dtype=torch.bool))
        out = torch.where(inside, seg, out)
    return out

def _to_hsl(r, g, b):
    mx = torch.maximum(r, torch.maximum(g, b)); mn = torch.minimum(r, torch.minimum(g, b)); d = mx - mn; l = (mx + mn) / 2
    safe = torch.where(d < 1e-9, torch.ones_like(d), d)
    h = torch.where(mx == r, (g - b) / safe, torch.where(mx == g, (b - r) / safe + 2, (r - g) / safe + 4))
    h = torch.remainder(h * 60 + 360, 360)
    s = d / torch.clamp(1 - (2 * l - 1).abs(), min=1e-9)
    gray = d < 1e-9
    return torch.where(gray, torch.zeros_like(h), h), torch.where(gray, torch.zeros_like(s), s), l

def _from_hsl(h, s, l):
    c = (1 - (2 * l - 1).abs()) * s; x = c * (1 - (torch.remainder(h / 60, 2) - 1).abs()); m = l - c / 2; z = torch.zeros_like(c)
    sectors = [(c, x, z), (x, c, z), (z, c, x), (z, x, c), (x, z, c), (c, z, x)]
    r, g, b = sectors[5]
    for k in range(4, -1, -1):
        sel = h < 60 * (k + 1); r = torch.where(sel, sectors[k][0], r); g = torch.where(sel, sectors[k][1], g); b = torch.where(sel, sectors[k][2], b)
    return r + m, g + m, b + m

def render(rgb, raw, quantize='round'):
    """rgb: [B,3,H,W] in [0,1] (byte values / 255). raw: [B,52] raw parameter values. Returns [B,3,H,W]."""
    r0, g0, b0 = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    P = lambda i: raw[:, i][:, None, None]
    # stage 1 (ColorProcessor.Apply)
    exposure = 2 ** P(0); contrast = 1 + P(3) / 100; saturation = 1 + P(8) / 100; warmth, tint = P(1), P(2)
    def chan(v, gain): return (_linear_to_srgb(_srgb_to_linear(v) * gain) - .5) * contrast + .5
    r = chan(r0, exposure * 2 ** (warmth / 200 + tint / 400)); g = chan(g0, exposure * 2 ** (-tint / 400)); b = chan(b0, exposure * 2 ** (-warmth / 200 + tint / 400))
    luma = .2126 * r + .7152 * g + .0722 * b; l = luma.clamp(0, 1)
    shift = (P(5) / 100 * .3 * (1 - l) ** 2 * (1 - _smooth(.45, .7, l)) + P(4) / 100 * .3 * l * l * _smooth(.3, .6, l)
             + P(7) / 100 * .2 * (1 - _smooth(0, .3, l)) + P(6) / 100 * .2 * _smooth(.7, 1, l))
    r, g, b, luma = r + shift, g + shift, b + shift, luma + shift
    chroma = torch.maximum(r, torch.maximum(g, b)) - torch.minimum(r, torch.minimum(g, b))
    vivid = saturation * (1 + P(9) / 100 * (1 - chroma.clamp(0, 1)))
    r, g, b = (_q(luma + (c - luma) * vivid, quantize) for c in (r, g, b))
    # stage 2 (AdvancedColorProcessor.Apply): curves -> HSL -> Color Balance
    def knots(c): return torch.stack([torch.tensor(k, dtype=raw.dtype) + raw[:, 10 + 3 * c + j] for j, k in enumerate(KNOTS)], 1)
    def monotone(ys):
        out, prev = [], torch.zeros_like(ys[:, 0])
        for j in range(3): prev = torch.maximum(prev, ys[:, j]).clamp(max=1); out.append(prev)
        return torch.stack(out, 1)
    master = monotone(knots(0))
    r, g, b = (_curve(_curve(v, master), monotone(knots(c))) for v, c in ((r, 1), (g, 2), (b, 3)))
    hsl = raw[:, 22:46].reshape(-1, 8, 3)
    if bool((hsl != 0).any()):
        active = (hsl != 0).any(2).any(1)[:, None, None]
        h, s, ll = _to_hsl(r, g, b); dh = ds = dl = total = torch.zeros_like(h)
        for n, center in enumerate(HUE_CENTERS):
            dist = (h - center).abs(); dist = torch.minimum(dist, 360 - dist); w = (1 - dist / 60).clamp_min(0); total = total + w
            dh = dh + hsl[:, n, 0][:, None, None] * .6 * w; ds = ds + hsl[:, n, 1][:, None, None] / 100 * w; dl = dl + hsl[:, n, 2][:, None, None] / 100 * .5 * w
        nr, ng, nb = _from_hsl(torch.remainder(h + dh / total + 360, 360), (s * (1 + ds / total)).clamp(0, 1), (ll + dl / total * s).clamp(0, 1))
        r, g, b = torch.where(active, nr, r), torch.where(active, ng, g), torch.where(active, nb, b)
    x, y = raw[:, 46:52:2], raw[:, 47:52:2]; amount = torch.sqrt(x * x + y * y).clamp(max=100)
    if bool((amount > 0).any()):
        hue = torch.where(amount < 1e-9, torch.zeros_like(amount), torch.remainder(torch.atan2(y, x) * 180 / math.pi + 360, 360))
        l = (.2126 * r + .7152 * g + .0722 * b).clamp(0, 1); biased = l  # GradingBalance 0, GradingBlending 50 -> power 2
        weights = ((1 - biased) ** 2, 4 * biased * (1 - biased), biased ** 2)
        one = torch.ones_like(hue[:, 0])
        for wi in range(3):
            cr, cg, cb = _from_hsl(hue[:, wi], one, one * .5)
            scale = (amount[:, wi] / 100 * .25)[:, None, None] * weights[wi]
            r = r + (cr[:, None, None] - .5) * scale; g = g + (cg[:, None, None] - .5) * scale; b = b + (cb[:, None, None] - .5) * scale
    return torch.stack([_q(v, quantize) for v in (r, g, b)], 1)

def bgra_to_rgb(raw_bytes, size):
    t = torch.frombuffer(bytearray(raw_bytes), dtype=torch.uint8).reshape(size, size, 4).to(torch.float64) / 255
    return t[..., [2, 1, 0]].permute(2, 0, 1)
