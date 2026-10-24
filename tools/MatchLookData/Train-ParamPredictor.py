"""P2 training (protocol section 13). CPU only. Picks the step count from a 50-step timing within a 2-hour budget
(max 15000 steps), validates every 500 steps on a fixed validation set, keeps the best checkpoint by validation loss."""
import argparse, json, math, random, time, hashlib
from pathlib import Path
import torch
import importlib.util
_s = importlib.util.spec_from_file_location('pp', Path(__file__).with_name('ParamPredictor.py')); pp = importlib.util.module_from_spec(_s); _s.loader.exec_module(pp)
crt = pp.crt

def batch(content, recipes, pairs, units):
    a = torch.stack([content.rgb128[i] for i, _ in pairs]).float() / 255; b = torch.stack([content.rgb128[j] for _, j in pairs]).float() / 255
    return a, b, units

def losses(model, a, b, unit, lo, hi):
    raw = crt.unit_to_raw(unit, lo, hi)
    with torch.no_grad():
        ref = crt.render(b, raw, 'round'); gt = crt.render(a, raw, 'round')
    pred_unit = model(ref, a); pred = crt.render(a, crt.unit_to_raw(pred_unit, lo, hi), 'ste')
    lp = (pred_unit - unit).abs().mean(); li = pp.image_loss(pred, gt, a)
    return lp, li

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--out', type=Path, required=True)
    ap.add_argument('--threads', type=int, default=12); ap.add_argument('--budget-hours', type=float, default=2.); ap.add_argument('--max-steps', type=int, default=15000)
    a = ap.parse_args(); a.out.mkdir(parents=True, exist_ok=False); torch.set_num_threads(a.threads); torch.manual_seed(17); rng = random.Random(17)
    content = pp.Content(a.data / 'content-set'); recipes = pp.load_recipes(a.data / 'recipes.jsonl')
    names, lo, hi = crt.load_schema(a.data / 'param-schema.json'); lo, hi = lo.float(), hi.float()
    model = pp.ParamPredictor(); opt = torch.optim.Adam(model.parameters(), lr=1e-3)
    val = pp.fixed_set(content, recipes, 'validation', 200, seed=99)
    def step():
        pairs = pp.sample_pairs(content, 'train', 16, rng); units = recipes['train'][torch.randint(len(recipes['train']), (16,))]
        lp, li = losses(model, *batch(content, recipes, pairs, units), lo, hi); loss = lp + li
        opt.zero_grad(); loss.backward(); torch.nn.utils.clip_grad_norm_(model.parameters(), 1.); opt.step(); return float(lp), float(li)
    def validate():
        model.eval(); tot = [0., 0.]
        with torch.no_grad():
            for k in range(0, len(val), 25):
                chunk = val[k:k + 25]; a_ = torch.stack([content.rgb128[i] for i, _, _ in chunk]).float() / 255; b_ = torch.stack([content.rgb128[j] for _, j, _ in chunk]).float() / 255
                lp, li = losses(model, a_, b_, torch.stack([u for _, _, u in chunk]), lo, hi); tot[0] += float(lp) * len(chunk); tot[1] += float(li) * len(chunk)
        model.train(); return tot[0] / len(val), tot[1] / len(val)
    t = time.perf_counter()
    for _ in range(50): step()
    per_step = (time.perf_counter() - t) / 50
    total = min(a.max_steps, int(a.budget_hours * 3600 / per_step)); sched = torch.optim.lr_scheduler.CosineAnnealingLR(opt, total - 50, eta_min=1e-5)
    plan = dict(seconds_per_step=per_step, steps=total, threads=a.threads, params=sum(p.numel() for p in model.parameters()))
    (a.out / 'plan.json').write_text(json.dumps(plan, indent=1)); print(json.dumps(plan), flush=True)
    log = (a.out / 'log.jsonl').open('w'); best = math.inf; run = [0., 0.]; started = time.perf_counter()
    for s in range(50, total):
        lp, li = step(); sched.step(); run[0] += lp; run[1] += li
        if (s + 1) % 500 == 0 or s + 1 == total:
            vp, vi = validate(); n = 500 if (s + 1) % 500 == 0 else (s + 1) % 500
            row = dict(step=s + 1, train_param=run[0] / n, train_image=run[1] / n, val_param=vp, val_image=vi, val_total=vp + vi, lr=sched.get_last_lr()[0], minutes=(time.perf_counter() - started) / 60)
            run = [0., 0.]; log.write(json.dumps(row) + '\n'); log.flush(); print(json.dumps(row), flush=True)
            if vp + vi < best: best = vp + vi; torch.save(dict(model=model.state_dict(), step=s + 1, val_total=best), a.out / 'best.pt')
    torch.save(dict(model=model.state_dict(), step=total), a.out / 'last.pt')
    (a.out / 'done.json').write_text(json.dumps(dict(best_val_total=best, minutes=(time.perf_counter() - started) / 60, data_hash=hashlib.sha256((a.data / 'recipes.jsonl').read_bytes()).hexdigest())))

if __name__ == '__main__': main()
