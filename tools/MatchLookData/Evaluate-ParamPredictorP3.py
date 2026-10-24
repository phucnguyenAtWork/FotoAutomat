"""P3 evaluation (protocol section 17).
  --make-test <dir>   300 fixed test items (test content x test looks, seed 2027) + v4 triplet files for K=3 and K=1
  --ridge <file>      ridge baseline on colour statistics (6,000 train samples, K=3), predicting the 52 parameters
  --items <test-items.json> --model <best.pt> --v4-k3 <dir> --v4-k1 <dir> --ridge <file> --out <dir>    metrics, guardrail violations, criteria
Renders are float64 (identical to C#); model inputs rendered from 128 px content as in training; metrics at 256 px."""
import argparse, json, random
from pathlib import Path
import torch
import importlib.util
_s = importlib.util.spec_from_file_location('p3', Path(__file__).with_name('ParamPredictorP3.py')); p3 = importlib.util.module_from_spec(_s); _s.loader.exec_module(p3)
_e = importlib.util.spec_from_file_location('ev', Path(__file__).with_name('Evaluate-ParamPredictor.py')); ev = importlib.util.module_from_spec(_e); _e.loader.exec_module(ev)
pp, crt = p3.pp, p3.crt
HERE = Path(__file__).parent

def feats(x):
    q = torch.quantile(x.flatten(2), x.new_tensor([.05, .25, .5, .75, .95]), dim=2).permute(1, 2, 0).flatten(1)
    lab = pp.srgb_to_lab(x).flatten(2); return torch.cat([x.flatten(2).mean(2), x.flatten(2).std(2), q, lab.mean(2), lab.std(2)], 1)

def ridge_features(refs, target): fr = sum(feats(r) for r in refs) / len(refs); fa = feats(target); return torch.cat([fr, fa, fr - fa], 1)

def setup(data):
    content = pp.Content(data / 'content-set'); names, lo, hi = crt.load_schema(data / 'param-schema.json')
    unit_by_id, splits = p3.load_looks(HERE / 'look-library-v1.json', HERE / 'look-splits-p3.json', names, lo, hi); return content, names, lo, hi, unit_by_id, splits

def violation(m): return m.get('skin_l_err', 0) > 8 or m.get('skin_clip_excess', 0) > .01

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--make-test', type=Path); ap.add_argument('--ridge', type=Path)
    ap.add_argument('--train-ridge', action='store_true'); ap.add_argument('--items', type=Path); ap.add_argument('--model', type=Path); ap.add_argument('--v4-k3', type=Path); ap.add_argument('--v4-k1', type=Path); ap.add_argument('--out', type=Path)
    a = ap.parse_args(); content, names, lo, hi, unit_by_id, splits = setup(a.data)
    if a.make_test:
        a.make_test.mkdir(parents=True, exist_ok=False); rng, gen = random.Random(2027), torch.Generator().manual_seed(2027)
        portraits, scenes, pool = content.pool('test', 'portrait'), content.pool('test', 'scene'), content.pool('test'); items = []
        for k in range(300):
            ia = rng.choice(portraits if rng.random() < .7 else scenes); refs = rng.sample([j for j in pool if j != ia], 3)
            u = p3.look_sample(unit_by_id, splits['test'], rng, gen)
            items.append(dict(k=k, a=content.items[ia]['id'], b=[content.items[j]['id'] for j in refs], size=256, unit=[round(float(v), 6) for v in u]))
        (a.make_test / 'test-items.json').write_text(json.dumps(items))
        (a.make_test / 'v4-k3.json').write_text(json.dumps(items)); (a.make_test / 'v4-k1.json').write_text(json.dumps([dict(t, b=t['b'][0]) for t in items]))
        print(f'{len(items)} test items'); return
    if a.train_ridge:
        rng, gen = random.Random(5), torch.Generator().manual_seed(5); recipes = pp.load_recipes(a.data / 'recipes.jsonl')
        pools = dict(portrait=content.pool('train', 'portrait'), scene=content.pool('train', 'scene'), all=content.pool('train')); X, Y = [], []
        # same sampling as training: 80% augmented train looks, 20% random recipes (train content only)
        import importlib.util as iu; s = iu.spec_from_file_location('t3', HERE / 'Train-ParamPredictorP3.py'); t3 = iu.module_from_spec(s); s.loader.exec_module(t3)
        lo32, hi32 = lo.float(), hi.float()
        for _ in range(6000 // 32):
            units, a_idx, refs, _ = t3.sample_batch(content, pools, rng, gen, unit_by_id, splits['train'], recipes['train'], 3, 32, augment=True)
            raw = crt.unit_to_raw(units, lo32, hi32); target = t3.images(content, a_idx)
            X.append(ridge_features([crt.render(t3.images(content, b), raw, 'round') for b in refs], target)); Y.append(units)
        X, Y = torch.cat(X).double(), torch.cat(Y).double(); mu, sd = X.mean(0), X.std(0) + 1e-6; A = torch.cat([(X - mu) / sd, torch.ones(len(X), 1, dtype=X.dtype)], 1)
        W = torch.linalg.solve(A.T @ A + 10 * torch.eye(A.shape[1], dtype=X.dtype), A.T @ Y); torch.save(dict(mu=mu, sd=sd, W=W), a.ridge); print('ridge saved'); return
    items = json.loads(a.items.read_text())
    model = p3.LookPredictor().double(); model.load_state_dict({k: v.double() for k, v in torch.load(a.model, weights_only=True)['model'].items()}); model.eval()
    rd = torch.load(a.ridge, weights_only=True)
    a.out.mkdir(parents=True, exist_ok=False); rows = []
    with torch.no_grad():
        for t in items:
            ia = content.index[t['a']]; raw = crt.unit_to_raw(torch.tensor([t['unit']], dtype=torch.float64), lo, hi)
            a256 = content.rgb256[ia][None].double() / 255; a128 = content.rgb128[ia][None].double() / 255; gt = crt.render(a256, raw)
            # model/ridge inputs rendered from the 128 px content, exactly as in training (rendering does not commute with pooling)
            refs128 = [crt.render(content.rgb128[content.index[b]][None].double() / 255, raw) for b in t['b']]
            row = dict(k=t['k'], kind_a=content.items[ia]['kind'], nothing=ev.metrics(a256, gt, a256))
            for K, refs in (('k3', refs128), ('k1', refs128[:1])):
                pu, _ = model(refs, a128); row[f'model_{K}'] = ev.metrics(crt.render(a256, crt.unit_to_raw(pu, lo, hi)), gt, a256)
                ru = ((torch.cat([(ridge_features(refs, a128) - rd['mu']) / rd['sd'], torch.ones(1, 1, dtype=torch.float64)], 1) @ rd['W'])).clamp(-1, 1)
                row[f'ridge_{K}'] = ev.metrics(crt.render(a256, crt.unit_to_raw(ru, lo, hi)), gt, a256)
                v4dir = a.v4_k3 if K == 'k3' else a.v4_k1
                row[f'v4_{K}'] = ev.metrics(crt.bgra_to_rgb((v4dir / f"{t['k']}.v4.bgra").read_bytes(), 256)[None], gt, a256)
            rows.append(row)
    methods = [m for m in rows[0] if m not in ('k', 'kind_a')]
    summary = {m: dict(de=ev.mean([r[m] for r in rows], 'de'), skin_de=ev.mean([r[m] for r in rows], 'skin_de'), skin_l_err=ev.mean([r[m] for r in rows], 'skin_l_err'),
                       violations=sum(violation(r[m]) for r in rows) / len(rows)) for m in methods}
    s = summary; gain = (s['nothing']['de'] - s['model_k3']['de']) / s['nothing']['de']
    criteria = dict(beats_nothing=s['model_k3']['de'] < s['nothing']['de'] and s['model_k3']['skin_de'] < s['nothing']['skin_de'],
                    beats_v4=s['model_k3']['de'] < s['v4_k3']['de'] and s['model_k3']['skin_de'] < s['v4_k3']['skin_de'],
                    beats_ridge=s['model_k3']['de'] < s['ridge_k3']['de'] and s['model_k3']['skin_de'] < s['ridge_k3']['skin_de'],
                    violations_at_most_2pct=s['model_k3']['violations'] <= .02, gain_vs_nothing_at_least_15pct=gain >= .15)
    criteria['pass'] = all(criteria.values()); summary['criteria'] = criteria; summary['gain_vs_nothing_k3'] = gain
    (a.out / 'summary.json').write_text(json.dumps(summary, indent=1)); (a.out / 'rows.json').write_text(json.dumps(rows)); print(json.dumps(summary, indent=1))

if __name__ == '__main__': main()
