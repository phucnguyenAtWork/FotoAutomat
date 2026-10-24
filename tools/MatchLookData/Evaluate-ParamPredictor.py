"""P2 evaluation (protocol section 13). Run once after training.
  --make-triplets <file>        fixed test set: 300 test pairs + 50 scene-reference identity pairs (seed 2026)
  --model <best.pt> --v4 <dir>  metrics for model and v4 on that set, criteria check, sample sheet
All renders are float64 (identical to C#). Inputs to the model are 128 px; metrics are measured at 256 px."""
import argparse, json
from pathlib import Path
import torch, torch.nn.functional as F
from PIL import Image
import importlib.util
_s = importlib.util.spec_from_file_location('pp', Path(__file__).with_name('ParamPredictor.py')); pp = importlib.util.module_from_spec(_s); _s.loader.exec_module(pp)
crt = pp.crt
GROUPS = dict(normalize=(0, 3), light=(3, 8), color=(8, 10), curves=(10, 22), hsl=(22, 46), balance=(46, 52))

def metrics(pred, gt, source):
    lab_p, lab_g = pp.srgb_to_lab(pred), pp.srgb_to_lab(gt); de = (lab_p - lab_g).norm(dim=1)[0]; skin = pp.skin_mask(source)[0] > 0
    out = dict(de=float(de.mean()))
    if int(skin.sum()) >= 50:
        out.update(skin_de=float(de[skin].mean()), skin_l_err=float((lab_p[0, 0] - lab_g[0, 0])[skin].abs().mean()),
                   skin_l_bias=float((lab_p[0, 0] - lab_g[0, 0])[skin].mean()),
                   skin_clip_excess=max(0., float((lab_p[0, 0][skin] > 97).float().mean() - (lab_g[0, 0][skin] > 97).float().mean())))
    return out

def mean(rows, key): vals = [r[key] for r in rows if key in r]; return sum(vals) / len(vals) if vals else None

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--make-triplets', type=Path)
    ap.add_argument('--triplets', type=Path); ap.add_argument('--model', type=Path); ap.add_argument('--v4', type=Path); ap.add_argument('--out', type=Path); a = ap.parse_args()
    content = pp.Content(a.data / 'content-set'); recipes = pp.load_recipes(a.data / 'recipes.jsonl')
    if a.make_triplets:
        rows = pp.fixed_set(content, recipes, 'test', 300, seed=2026, scene_identity=50)
        out = [dict(k=k, a=content.items[i]['id'], b=content.items[j]['id'], size=256, unit=[round(float(v), 6) for v in u], scene_identity=k >= 300) for k, (i, j, u) in enumerate(rows)]
        a.make_triplets.write_text(json.dumps(out)); print(f'{len(out)} triplets'); return
    names, lo, hi = crt.load_schema(a.data / 'param-schema.json'); triplets = json.loads(a.triplets.read_text())
    model = pp.ParamPredictor().double(); model.load_state_dict({k: v.double() for k, v in torch.load(a.model, weights_only=True)['model'].items()}); model.eval()
    v4 = {r['k']: r for r in json.loads((a.v4 / 'v4.json').read_text())}
    a.out.mkdir(parents=True, exist_ok=False); rows, sheet = [], []
    with torch.no_grad():
        for t in triplets:
            ia, ib = content.index[t['a']], content.index[t['b']]; unit = torch.tensor([t['unit']], dtype=torch.float64); raw = crt.unit_to_raw(unit, lo, hi)
            a256 = content.rgb256[ia][None].double() / 255; ref256 = crt.render(content.rgb256[ib][None].double() / 255, raw); gt = crt.render(a256, raw)
            ref128 = (F.avg_pool2d(ref256 * 255, 2).round()) / 255; a128 = content.rgb128[ia][None].double() / 255
            pu = model(ref128, a128); pred = crt.render(a256, crt.unit_to_raw(pu, lo, hi))
            v4img = crt.bgra_to_rgb((a.v4 / f"{t['k']}.v4.bgra").read_bytes(), 256)[None]
            row = dict(k=t['k'], scene_identity=t['scene_identity'], kind_a=content.items[ia]['kind'], kind_b=content.items[ib]['kind'],
                       model={**metrics(pred, gt, a256), **{f'param_{g}': float((pu[0, s:e] - unit[0, s:e]).abs().mean()) for g, (s, e) in GROUPS.items()}},
                       v4={**metrics(v4img, gt, a256), **{f'param_{g}': float((torch.tensor(v4[t['k']]['unit'][s:e]) - unit[0, s:e]).abs().mean()) for g, (s, e) in GROUPS.items()}, 'ms': v4[t['k']]['ms']})
            rows.append(row)
            if len(sheet) < 12 and not t['scene_identity'] and t['k'] % 7 == 0: sheet.append((a256, ref256, gt, pred, v4img))
    main_rows = [r for r in rows if not r['scene_identity']]; ident = [r for r in rows if r['scene_identity']]
    summary = {}
    for name, subset in (('test300', main_rows), ('scene_identity50', ident), ('test300_portrait_target', [r for r in main_rows if r['kind_a'] == 'portrait']), ('test300_scene_reference', [r for r in main_rows if r['kind_b'] == 'scene'])):
        summary[name] = {m: {key: mean([r[m] for r in subset], key) for key in ('de', 'skin_de', 'skin_l_err', 'skin_l_bias', 'skin_clip_excess', *[f'param_{g}' for g in GROUPS])} for m in ('model', 'v4')}
        summary[name]['n'] = len(subset)
    s, i = summary['test300'], summary['scene_identity50']
    summary['criteria'] = dict(de=s['model']['de'] < s['v4']['de'], skin_de=s['model']['skin_de'] < s['v4']['skin_de'], skin_l_err=s['model']['skin_l_err'] < s['v4']['skin_l_err'],
                               scene_identity_not_worse=i['model']['de'] <= i['v4']['de'])
    summary['criteria']['pass'] = all(summary['criteria'].values()); summary['v4_ms_mean'] = mean([r['v4'] for r in rows], 'ms')
    (a.out / 'summary.json').write_text(json.dumps(summary, indent=1)); (a.out / 'rows.json').write_text(json.dumps(rows))
    tiles = Image.new('RGB', (5 * 160, len(sheet) * 160))
    for y, imgs in enumerate(sheet):
        for x, im in enumerate(imgs):
            tiles.paste(Image.fromarray((im[0].permute(1, 2, 0).clamp(0, 1) * 255).round().byte().numpy()).resize((160, 160)), (x * 160, y * 160))
    tiles.save(a.out / 'samples-target-reference-truth-model-v4.jpg', quality=88)
    print(json.dumps(summary, indent=1))

if __name__ == '__main__': main()
