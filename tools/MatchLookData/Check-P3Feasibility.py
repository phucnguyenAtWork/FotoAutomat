"""P3 pre-training feasibility check (protocol section 17). Report only; no network is trained.
Ridge (lambda 10) on colour statistics of K references (averaged) + target, predicting the 52 parameters and the look
signature (64-colour palette through the recipe, Lab). Samples use library looks only (training itself adds 20% random).
  registered: train content x train looks (6,000) -> A: validation content x train looks, B: validation content x validation looks
  --augment: training samples from look_augment (option b); evaluation samples are never augmented
  --lolo (outside the registration): leave-one-look-out over the 25 non-test looks (train content -> validation content)
Usage: Check-P3Feasibility.py --data <param-dataset dir> [--lolo]"""
import argparse, json, random
from pathlib import Path
import torch, importlib.util
_s = importlib.util.spec_from_file_location('ev3', Path(__file__).with_name('Evaluate-ParamPredictorP3.py')); ev3 = importlib.util.module_from_spec(_s); _s.loader.exec_module(ev3)
p3, pp, crt = ev3.p3, ev3.pp, ev3.crt
g = torch.linspace(0, 1, 4); PALETTE = torch.stack(torch.meshgrid(g, g, g, indexing='ij')).reshape(3, 8, 8)[None]

def sample(content, unit_by_id, lo, hi, n, split, looks, seed, augment=False):
    """n samples (multiple of 25); returns ({K: features}, units, signatures, set of look ids per sample)."""
    rng, gen = random.Random(seed), torch.Generator().manual_seed(seed)
    por, sce, pool = content.pool(split, 'portrait'), content.pool(split, 'scene'), content.pool(split); X = {1: [], 3: [], 5: []}; Y, S, L = [], [], []
    for _ in range(n // 25):
        if augment: units, ids = zip(*[p3.look_augment(unit_by_id, looks, rng, gen) for _ in range(25)]); units, ids = torch.stack(units), [set(i) for i in ids]
        else: ids = [{rng.choice(looks)} for _ in range(25)]; units = torch.stack([p3.look_sample(unit_by_id, list(i), rng, gen) for i in ids])
        raw = crt.unit_to_raw(units, lo, hi)
        a_idx = [rng.choice(por if rng.random() < .7 else sce) for _ in range(25)]; a = torch.stack([content.rgb128[i] for i in a_idx]).float() / 255
        refs = [crt.render(torch.stack([content.rgb128[rng.choice([j for j in pool if j != i])] for i in a_idx]).float() / 255, raw, 'round') for _ in range(5)]
        for k in X: X[k].append(ev3.ridge_features(refs[:k], a))
        Y.append(units); S.append(pp.srgb_to_lab(crt.render(PALETTE.expand(25, -1, -1, -1), raw, 'round')).flatten(1)); L += ids
    return {k: torch.cat(v).double() for k, v in X.items()}, torch.cat(Y).double(), torch.cat(S).double(), L

def fit(X, Y):
    mu, sd = X.mean(0), X.std(0) + 1e-6; A = torch.cat([(X - mu) / sd, torch.ones(len(X), 1, dtype=X.dtype)], 1)
    return mu, sd, torch.linalg.solve(A.T @ A + 10 * torch.eye(A.shape[1], dtype=X.dtype), A.T @ Y)
def predict(m, X): mu, sd, W = m; return torch.cat([(X - mu) / sd, torch.ones(len(X), 1, dtype=X.dtype)], 1) @ W
def r2(p, y):
    var = y.var(0, unbiased=False); ok = var > 1e-8; return 1 - ((p - y) ** 2).mean(0)[ok] / var[ok], ok
def sig_de(q, s): return float((q - s).reshape(len(q), 3, -1).norm(dim=1).mean())  # Lab is channel-major
IDENT = lambda s: pp.srgb_to_lab(PALETTE).flatten(1).double().expand_as(s)

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--lolo', action='store_true'); ap.add_argument('--augment', action='store_true', help='training samples from look_augment (option b)'); ap.add_argument('--threads', type=int, default=12)
    a = ap.parse_args(); torch.set_num_threads(a.threads)
    content, names, lo, hi, unit_by_id, splits = ev3.setup(a.data); lo, hi = lo.float(), hi.float(); out = {}
    if not a.lolo:
        tr = sample(content, unit_by_id, lo, hi, 6000, 'train', splits['train'], 1, a.augment)
        evs = dict(A_new_content_known_looks=sample(content, unit_by_id, lo, hi, 1000, 'validation', splits['train'], 2),
                   B_unseen_looks=sample(content, unit_by_id, lo, hi, 1000, 'validation', splits['validation'], 3))
        for k in (1, 3, 5):
            mp, ms = fit(tr[0][k], tr[1]), fit(tr[0][k], tr[2])
            for tag, e in evs.items():
                r, ok = r2(predict(mp, e[0][k]), e[1]); by = dict(zip([names[i] for i in ok.nonzero().flatten().tolist()], r.tolist()))
                out[f'K{k}_{tag}'] = dict(params_median_r2=round(float(r.median()), 3), params_with_variance=int(ok.sum()), **{n: round(by.get(n, float('nan')), 3) for n in ('Exposure', 'Warmth', 'Tint')},
                                          signature_dE_ridge=round(sig_de(predict(ms, e[0][k]), e[2]), 2), signature_dE_do_nothing=round(sig_de(IDENT(e[2]), e[2]), 2),
                                          signature_dE_train_mean=round(sig_de(tr[2].mean(0).expand_as(e[2]), e[2]), 2))
    else:
        looks = splits['train'] + splits['validation']; tr = sample(content, unit_by_id, lo, hi, 25 * 250, 'train', looks, 11, a.augment); ev = sample(content, unit_by_id, lo, hi, 25 * 40, 'validation', looks, 12)
        for k in (1, 3, 5):
            per = {}
            for held in looks:
                ti = torch.tensor([held not in l for l in tr[3]]); ei = torch.tensor([held in l for l in ev[3]])
                if not ei.any(): continue
                ms = fit(tr[0][k][ti], tr[2][ti]); s = ev[2][ei]
                per[held] = dict(ridge=sig_de(predict(ms, ev[0][k][ei]), s), do_nothing=sig_de(IDENT(s), s), train_mean=sig_de(tr[2][ti].mean(0).expand_as(s), s))
            m = lambda key: round(sum(v[key] for v in per.values()) / len(per), 2)
            out[f'K{k}_leave_one_look_out'] = dict(looks=len(per), signature_dE_ridge=m('ridge'), signature_dE_do_nothing=m('do_nothing'), signature_dE_train_mean=m('train_mean'),
                                                   looks_where_ridge_beats_do_nothing=sum(v['ridge'] < v['do_nothing'] for v in per.values()),
                                                   per_look={l: {x: round(y, 2) for x, y in v.items()} for l, v in per.items()} if k == 5 else None)
    print(json.dumps(out, indent=1, ensure_ascii=False))

if __name__ == '__main__': main()
