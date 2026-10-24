"""Download selected CVDF images unchanged; generate local review gallery."""
import argparse
import hashlib
import html
import json
import re
import subprocess
from collections import Counter
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--selection', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    records = json.loads(args.selection.read_text(encoding='utf-8-sig'))
    if args.output.exists():
        raise ValueError('Output must be new')
    if len({r['ImageID'] for r in records}) != len(records):
        raise ValueError('Duplicate IDs')
    for r in records:
        if not re.fullmatch('[a-f0-9]{16}', r['ImageID']) or r['Subset'] != 'train':
            raise ValueError('Invalid selected ID/split')
    root = args.output
    (root/'images').mkdir(parents=True)
    (root/'thumbnails').mkdir()
    (root/'selection.json').write_bytes(args.selection.read_bytes())
    def fetch(record):
        r = dict(record); name = r['ImageID']
        url = f'https://open-images-dataset.s3.amazonaws.com/train/{name}.jpg'
        path = root/'images'/f'{name}.jpg'; partial = path.with_suffix('.part')
        r.update(download_url=url, training_ready=False)
        try:
            run = subprocess.run(['curl.exe','--silent','--show-error','--fail','--location',
                '--retry','2','--connect-timeout','15','--max-time','90','--output',str(partial),url],
                capture_output=True, text=True, timeout=300)
            if run.returncode:
                raise RuntimeError(run.stderr.strip())
            with Image.open(partial) as im:
                im.load()
                r.update(width=im.width,height=im.height,icc_present=bool(im.info.get('icc_profile')))
                preview = ImageOps.exif_transpose(im).convert('RGB')
                preview.thumbnail((320,240))
                preview.save(root/'thumbnails'/f'{name}.jpg',quality=85)
            r.update(sha256=hashlib.sha256(partial.read_bytes()).hexdigest(),bytes=partial.stat().st_size,
                status='downloaded', path=f'images/{name}.jpg')
            partial.rename(path)
        except Exception as error:
            r.update(status='failed',error=str(error))
            partial.unlink(missing_ok=True)
        return r
    completed = {}
    with (root/'download-log.jsonl').open('w',encoding='utf-8') as log:
        with ThreadPoolExecutor(max_workers=5) as pool:
            for future in as_completed([pool.submit(fetch,r) for r in records]):
                row = future.result(); completed[row['ImageID']] = row
                log.write(json.dumps(row,ensure_ascii=False)+'\n'); log.flush()
                if len(completed)%50 == 0:
                    print(f'{len(completed)}/{len(records)} processed',flush=True)
    results = [completed[r['ImageID']] for r in records]
    good = [r for r in results if r['status']=='downloaded']
    groups = Counter(r['review_group'] for r in good)
    summary = dict(requested=len(records),downloaded=len(good),failed=len(records)-len(good),
        bytes=sum(r['bytes'] for r in good),groups=dict(groups),training_ready=False,
        selection_sha256=hashlib.sha256(args.selection.read_bytes()).hexdigest(),
        note='Unmodified CVDF download bytes; not necessarily original Flickr resolution. Local SHA256 is not upstream hash verification. Previews resized/EXIF-oriented, no ICC conversion. Rights and visual review pending.')
    (root/'manifest.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    (root/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    pages = []
    for start in range(0,len(good),48):
        batch = good[start:start+48]; page = start//48+1
        sheet = Image.new('RGB',(1280,((len(batch)+3)//4)*285),'#202020'); draw=ImageDraw.Draw(sheet)
        cards=[]
        for j,r in enumerate(batch):
            name=r['ImageID']; x=(j%4)*320; y=(j//4)*285
            with Image.open(root/'thumbnails'/f'{name}.jpg') as im: sheet.paste(im,(x,y))
            draw.text((x+4,y+243),f'{start+j+1} {name}',fill='white')
            draw.text((x+4,y+259),r['review_group'],fill='white')
            cards.append(f'<article data-group="{html.escape(r["review_group"])}"><a href="images/{name}.jpg"><img loading="lazy" src="thumbnails/{name}.jpg"></a><p>{start+j+1}. {name} · {r["review_group"]} · {r["width"]}×{r["height"]}</p><p>{html.escape(r["Author"])} — <a href="{html.escape(r["OriginalLandingURL"],quote=True)}">Source</a> · <a href="{html.escape(r["License"],quote=True)}">License</a></p></article>')
        sheet.save(root/f'contact-sheet-{page:02}.jpg',quality=85)
        pages.append(''.join(cards))
    nav=' '.join(f'<a href="review-{i+1:02}.html">{i+1}</a>' for i in range(len(pages)))
    for i,cards in enumerate(pages):
        (root/f'review-{i+1:02}.html').write_text('<!doctype html><meta charset="utf-8"><title>Open Images review</title><style>body{background:#202020;color:#eee;font:14px system-ui;margin:24px}a{color:#8bd7db}nav{display:flex;gap:14px;flex-wrap:wrap}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(270px,1fr));gap:16px}img{width:100%;height:240px;object-fit:contain}article{background:#303030;padding:10px}p{overflow-wrap:anywhere}</style><h1>Open Images · review candidates</h1><p>Pending visual and rights review. Not training-ready. Thumbnails resized; color profiles not normalized.</p><nav>'+nav+'</nav><main>'+cards+'</main>',encoding='utf-8')
    (root/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Open Images</title><h1>Open Images: '+str(len(good))+' candidates</h1><p>Review pending. No training performed.</p>'+nav,encoding='utf-8')
    print(json.dumps(summary,indent=2),flush=True)

if __name__ == '__main__': main()
