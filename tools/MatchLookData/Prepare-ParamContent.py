"""Content images for the Match Look parameter predictor (protocol: docs/ai-match-look/param-predictor-protocol-2026-10-16.md).

Decodes each source once (EXIF orientation applied), resizes the short side to SIZE and centre-crops a square, and writes
raw 8-bit BGRA (the byte layout ColorProcessor uses) plus a manifest with source, licence/attribution, kind
(portrait/scene), hashes and a split. Splits are made per group (booth session or Open Images author) so one session or
photographer never sits in two splits. Booth test/quarantine assets and the historic test reserve are excluded.
No colour conversion or grading is done here; colour work stays in ColorProcessor.
"""
import argparse, hashlib, json, sys
from pathlib import Path
from PIL import Image, ImageOps

SIZE = 256

def split_of(group: str) -> str:
    h = int(hashlib.sha256(group.encode('utf-8')).hexdigest(), 16) % 100
    return 'test' if h < 10 else 'validation' if h < 20 else 'train'

def to_bgra(path: Path) -> bytes:
    with Image.open(path) as im:
        im = ImageOps.exif_transpose(im).convert('RGB')
        w, h = im.size; s = SIZE / min(w, h)
        im = im.resize((max(SIZE, round(w * s)), max(SIZE, round(h * s))), Image.Resampling.LANCZOS)
        w, h = im.size; left, top = (w - SIZE) // 2, (h - SIZE) // 2
        im = im.crop((left, top, left + SIZE, top + SIZE))
        r, g, b = im.split(); a = Image.new('L', im.size, 255)
        return Image.merge('RGBA', (b, g, r, a)).tobytes()  # stored as B,G,R,A

def sources(data: Path):
    for row in json.loads((data / 'openimages-review-001/manifest.json').read_text(encoding='utf-8')):
        if row.get('status') not in (None, 'ok', 'downloaded') or not row.get('path'): continue
        yield dict(id='oi-' + row['ImageID'], path=data / 'openimages-review-001' / row['path'] if not Path(row['path']).is_absolute() else Path(row['path']),
                   source='openimages', kind='portrait' if row['review_group'] == 'portrait' else 'scene', scene=row['review_group'],
                   group='oi-author:' + row.get('AuthorProfileURL', row['ImageID']), license=row['License'],
                   attribution=f"{row.get('Author', '')} — {row.get('OriginalLandingURL', '')}")
    pack = data / 'finetune-prep-2026-10-12-002'
    for a in json.loads((pack / 'curriculum.ready.json').read_text(encoding='utf-8'))['assets']:
        if a['split'] in ('test', 'quarantine') or a.get('historic_test_reserved'): continue
        yield dict(id='booth-' + a['id'][:16], path=pack / a['file']['path'].replace('\\', '/'), source='booth', kind='portrait', scene='booth',
                   group='booth:' + a['group'], license='user_confirmed', attribution='FotoAutomat booth (user-confirmed rights)')

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--data', type=Path, required=True); ap.add_argument('--out', type=Path, required=True); a = ap.parse_args()
    (a.out / 'content').mkdir(parents=True, exist_ok=False)
    rows, skipped = [], []
    for s in sources(a.data):
        if not s['path'].exists(): skipped.append(dict(id=s['id'], reason='missing file')); continue
        try: raw = to_bgra(s['path'])
        except Exception as e: skipped.append(dict(id=s['id'], reason=str(e)[:120])); continue
        (a.out / 'content' / (s['id'] + '.bgra')).write_bytes(raw)
        rows.append(dict({k: (str(v) if isinstance(v, Path) else v) for k, v in s.items()}, size=SIZE, split=split_of(s['group']),
                         source_sha256=hashlib.sha256(s['path'].read_bytes()).hexdigest(), bgra_sha256=hashlib.sha256(raw).hexdigest()))
    summary = dict(count=len(rows), skipped=len(skipped), size=SIZE, layout='BGRA8 row-major, SIZE x SIZE',
                   by_split_kind={f"{sp}/{k}": sum(r['split'] == sp and r['kind'] == k for r in rows) for sp in ('train', 'validation', 'test') for k in ('portrait', 'scene')},
                   by_source={src: sum(r['source'] == src for r in rows) for src in ('openimages', 'booth')})
    (a.out / 'content-manifest.json').write_text(json.dumps(dict(summary=summary, items=rows, skipped=skipped), ensure_ascii=False, indent=1), encoding='utf-8')
    print(json.dumps(summary, indent=1))

if __name__ == '__main__': main()
