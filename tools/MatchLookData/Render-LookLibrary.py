"""Review sheet for the portrait look library (protocol section 16, step 2).
Renders every look on a few booth portraits (float64 renderer, identical to C# ColorProcessor) and reports skin guardrail
metrics per look (option A): mean skin L* shift, skin hue shift, skin saturation change, newly clipped skin pixels.
Usage: Render-LookLibrary.py --data <param-dataset dir> --library look-library-v1.json --out <new dir>"""
import argparse, html, json, math
from pathlib import Path
import torch
from PIL import Image
import importlib.util
_s = importlib.util.spec_from_file_location('pp', Path(__file__).with_name('ParamPredictor.py')); pp = importlib.util.module_from_spec(_s); _s.loader.exec_module(pp)
crt = pp.crt

def to_raw(params, names, lo, hi):
    """Library entry -> 52 raw values; refuses values outside the guardrail ranges."""
    raw = torch.zeros(len(names), dtype=torch.float64); index = {n: i for i, n in enumerate(names)}
    for key, value in params.items():
        if key.startswith('Balance.'):
            a = math.radians(value['hue']); pairs = [(f'{key}.X', value['amount'] * math.cos(a)), (f'{key}.Y', value['amount'] * math.sin(a))]
        elif key.startswith('Curve.'): pairs = [(f'{key}.{k}', v) for k, v in zip(('0.25', '0.50', '0.75'), value)]
        else: pairs = [(key, value)]
        for name, v in pairs:
            i = index[name]
            if not lo[i] - 1e-9 <= v <= hi[i] + 1e-9: raise ValueError(f'{name}={v} outside guardrail [{lo[i]}, {hi[i]}]')
            raw[i] = v
    return raw

def skin_metrics(before, after):
    skin = pp.skin_mask(before)[0] > 0
    if int(skin.sum()) < 50: return None
    lb, la = pp.srgb_to_lab(before)[0], pp.srgb_to_lab(after)[0]
    hue = lambda lab: torch.atan2(lab[2], lab[1]); dh = (hue(la) - hue(lb) + math.pi) % (2 * math.pi) - math.pi
    chroma = lambda lab: (lab[1] ** 2 + lab[2] ** 2).sqrt()
    return dict(l_shift=float((la[0] - lb[0])[skin].mean()), hue_shift=float(dh[skin].abs().mean() * 180 / math.pi),
                chroma_change=float((chroma(la) - chroma(lb))[skin].mean()), new_clip=float(((la[0] > 97) & ~(lb[0] > 97))[skin].float().mean() * 100))

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--library', type=Path, required=True); ap.add_argument('--out', type=Path, required=True)
    ap.add_argument('--images', type=int, default=5); a = ap.parse_args(); a.out.mkdir(parents=True, exist_ok=False)
    content = pp.Content(a.data / 'content-set'); names, lo, hi = crt.load_schema(a.data / 'param-schema.json')
    booth = [i for i, r in enumerate(content.items) if r['source'] == 'booth']
    picks = [booth[k * len(booth) // (a.images - 1)] for k in range(a.images - 1)] + [next(i for i, r in enumerate(content.items) if r['source'] == 'openimages' and r['kind'] == 'portrait')]
    looks = json.loads(a.library.read_text(encoding='utf-8'))['looks']
    rows = [dict(id='original', name='Ảnh gốc (chưa chỉnh)', family='', params={})] + looks; report = []
    sheet = Image.new('RGB', (160 * len(picks), 160 * len(rows)))
    for y, look in enumerate(rows):
        raw = to_raw(look['params'], names, lo, hi)[None]; metrics = []
        for x, i in enumerate(picks):
            src = content.rgb256[i][None].double() / 255; out = crt.render(src, raw)
            img = Image.fromarray((out[0].permute(1, 2, 0) * 255).round().byte().numpy()).resize((192, 192), Image.Resampling.LANCZOS)
            img.save(a.out / f"{look['id']}-{x}.jpg", quality=90); sheet.paste(img.resize((160, 160)), (x * 160, y * 160))
            if y: m = skin_metrics(src, out); metrics.append(m) if m else None
        avg = {k: sum(m[k] for m in metrics) / len(metrics) for k in metrics[0]} if metrics else {}
        flags = [f for f, bad in (('da sáng/tối > 8 L*', abs(avg.get('l_shift', 0)) > 8), ('lệch hue da > 8°', avg.get('hue_shift', 0) > 8), ('da cháy mới > 1%', avg.get('new_clip', 0) > 1)) if bad]
        report.append(dict(id=look['id'], name=look['name'], family=look['family'], skin=avg, flags=flags, params=look['params']))
    sheet.save(a.out / 'contact.jpg', quality=85)
    (a.out / 'report.json').write_text(json.dumps(dict(images=[content.items[i]['id'] for i in picks], looks=report), ensure_ascii=False, indent=1), encoding='utf-8')
    css = 'body{font:14px system-ui;background:#1d1f21;color:#ddd;margin:20px}table{border-collapse:collapse}td{padding:4px;vertical-align:middle;border-bottom:1px solid #333}img{width:150px;height:150px;display:block}.warn{color:#f0a04b}.meta{font-size:12px;color:#999;max-width:260px}'
    out = [f'<!doctype html><meta charset="utf-8"><title>Thư viện look chân dung v1</title><style>{css}</style><h1>Thư viện look chân dung v1 — bản duyệt</h1>',
           '<p>Mỗi hàng là một look áp lên cùng các ảnh (render khớp app). Cột phải: chỉ số da trung bình (phương án A). Đánh dấu look nào giữ / bỏ / cần chỉnh.</p><table>']
    for row in report[:1] + report[1:]: pass
    for look, entry in zip(rows, [None] + report):
        cells = ''.join(f'<td><img src="{look["id"]}-{x}.jpg"></td>' for x in range(len(picks)))
        meta = '' if entry is None else (f"{html.escape(entry['family'])}<br>L* {entry['skin'].get('l_shift', 0):+.1f} · hue {entry['skin'].get('hue_shift', 0):.1f}° · chroma {entry['skin'].get('chroma_change', 0):+.1f}"
                                         + (f"<br><span class=warn>⚠ {html.escape(', '.join(entry['flags']))}</span>" if entry['flags'] else ''))
        out.append(f'<tr><td><b>{html.escape(look["name"])}</b><div class=meta>{meta}</div></td>{cells}</tr>')
    (a.out / 'index.html').write_text('\n'.join(out) + '</table>', encoding='utf-8')
    print(json.dumps([dict(id=r['id'], **{k: round(v, 1) for k, v in r['skin'].items()}, flags=r['flags']) for r in report], ensure_ascii=False))

if __name__ == '__main__': main()
