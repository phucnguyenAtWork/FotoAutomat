"""P3 training (protocol section 17). CPU only; run only after the user approves. Step count from a 50-step timing
within --budget-hours (max --max-steps). Validation every 500 steps on 200 fixed samples (validation content x
validation looks unaugmented, K = 3); training looks are augmented (section 17, option b); keeps the best checkpoint by validation loss. Test data is never touched here."""
import argparse, json, math, random, time, hashlib
from pathlib import Path
import torch
import importlib.util
_s = importlib.util.spec_from_file_location('p3', Path(__file__).with_name('ParamPredictorP3.py')); p3 = importlib.util.module_from_spec(_s); _s.loader.exec_module(p3)
pp, crt = p3.pp, p3.crt
W = dict(image=1., param=.2, economy=.02, conflict=.05, lpp=.1, skin_l=.5, clip=1.)  # fixed in protocol section 17

def images(content, idx): return torch.stack([content.rgb128[i] for i in idx]).float() / 255

def sample_batch(content, pools, rng, gen, unit_by_id, look_ids, random_units, k, n=16, augment=False):
    """augment: training looks from p3.look_augment (blends, control dropout, strength 0.3-1.3); otherwise single looks."""
    look = (lambda: p3.look_augment(unit_by_id, look_ids, rng, gen)[0]) if augment else (lambda: p3.look_sample(unit_by_id, look_ids, rng, gen))
    units = torch.stack([look() if rng.random() < .8 or random_units is None else random_units[rng.randrange(len(random_units))] for _ in range(n)])
    a_idx = [rng.choice(pools['portrait'] if rng.random() < .7 else pools['scene']) for _ in range(n)]
    def refs():  # K reference images per sample, each a different B, never A
        return [[rng.choice([j for j in pools['all'] if j != a]) for a in a_idx] for _ in range(k)]
    return units, a_idx, refs(), refs()

def step_losses(model, content, units, a_idx, refs1, refs2, lo, hi, with_lpp=True):
    raw = crt.unit_to_raw(units, lo, hi); a = images(content, a_idx)
    with torch.no_grad():
        gt = crt.render(a, raw, 'round'); r1 = [crt.render(images(content, b), raw, 'round') for b in refs1]
        r2 = [crt.render(images(content, b), raw, 'round') for b in refs2] if with_lpp else None
    pred_unit, z1 = model(r1, a); pred_raw = crt.unit_to_raw(pred_unit, lo, hi); pred = crt.render(a, pred_raw, 'ste')
    l_skin, l_clip = p3.skin_guard(pred, gt, a)
    parts = dict(image=pp.image_loss(pred, gt, a), param=(pred_unit - units).abs().mean(), economy=p3.economy(pred_unit),
                 conflict=p3.conflicts(pred_raw), skin_l=l_skin, clip=l_clip)
    if with_lpp:
        z2 = model.look_vector(r2, a); parts['lpp'] = ((z1 - z2) ** 2).mean() / ((z1 ** 2).mean() + (z2 ** 2).mean() + 1e-6)
    return parts

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--out', type=Path, required=True)
    ap.add_argument('--threads', type=int, default=12); ap.add_argument('--budget-hours', type=float, required=True); ap.add_argument('--max-steps', type=int, default=30000)
    a = ap.parse_args(); a.out.mkdir(parents=True, exist_ok=False); torch.set_num_threads(a.threads); torch.manual_seed(23); rng = random.Random(23); gen = torch.Generator().manual_seed(23)
    here = Path(__file__).parent; content = pp.Content(a.data / 'content-set'); recipes = pp.load_recipes(a.data / 'recipes.jsonl')
    names, lo, hi = crt.load_schema(a.data / 'param-schema.json')
    unit_by_id, splits = p3.load_looks(here / 'look-library-v1.json', here / 'look-splits-p3.json', names, lo, hi); lo, hi = lo.float(), hi.float()
    pools = {s: dict(portrait=content.pool(s, 'portrait'), scene=content.pool(s, 'scene'), all=content.pool(s)) for s in ('train', 'validation')}
    model = p3.LookPredictor(); opt = torch.optim.Adam(model.parameters(), lr=1e-3)
    vrng, vgen = random.Random(99), torch.Generator().manual_seed(99)
    val = [sample_batch(content, pools['validation'], vrng, vgen, unit_by_id, splits['validation'], None, 3, 25) for _ in range(8)]
    def train_step():
        k = rng.randint(1, 5); parts = step_losses(model, content, *sample_batch(content, pools['train'], rng, gen, unit_by_id, splits['train'], recipes['train'], k, augment=True), lo, hi)
        loss = sum(W[n] * v for n, v in parts.items()); opt.zero_grad(); loss.backward(); torch.nn.utils.clip_grad_norm_(model.parameters(), 1.); opt.step()
        return {n: float(v.detach()) for n, v in parts.items()}
    def validate():
        model.eval(); tot = {}
        with torch.no_grad():
            for b in val:
                for n, v in step_losses(model, content, *b, lo, hi, with_lpp=False).items(): tot[n] = tot.get(n, 0) + float(v) / len(val)
        model.train(); tot['total'] = sum(W[n] * v for n, v in tot.items()); return tot
    t = time.perf_counter()
    for _ in range(50): train_step()
    per_step = (time.perf_counter() - t) / 50; total = min(a.max_steps, int(a.budget_hours * 3600 / per_step))
    sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, total - 50, eta_min=1e-5)
    plan = dict(seconds_per_step=per_step, steps=total, threads=a.threads, params=sum(p.numel() for p in model.parameters()), weights=W)
    (a.out / 'plan.json').write_text(json.dumps(plan, indent=1)); print(json.dumps(plan), flush=True)
    log = (a.out / 'log.jsonl').open('w'); best = math.inf; run = {}; started = time.perf_counter()
    for s in range(50, total):
        for n, v in train_step().items(): run[n] = run.get(n, 0) + v
        sched.step()
        if (s + 1) % 500 == 0 or s + 1 == total:
            v = validate(); count = 500 if (s + 1) % 500 == 0 else (s + 1) % 500
            row = dict(step=s + 1, train={n: x / count for n, x in run.items()}, val=v, lr=sched.get_last_lr()[0], minutes=(time.perf_counter() - started) / 60)
            run = {}; log.write(json.dumps(row) + '\n'); log.flush(); print(json.dumps(dict(step=s + 1, val_total=round(v['total'], 4), val_image=round(v['image'], 4), minutes=round(row['minutes'], 1))), flush=True)
            if v['total'] < best: best = v['total']; torch.save(dict(model=model.state_dict(), step=s + 1, val_total=best), a.out / 'best.pt')
    torch.save(dict(model=model.state_dict(), step=total), a.out / 'last.pt')
    (a.out / 'done.json').write_text(json.dumps(dict(best_val_total=best, minutes=(time.perf_counter() - started) / 60,
        library_sha256=hashlib.sha256((here / 'look-library-v1.json').read_bytes()).hexdigest(), splits_sha256=hashlib.sha256((here / 'look-splits-p3.json').read_bytes()).hexdigest())))

if __name__ == '__main__': main()
