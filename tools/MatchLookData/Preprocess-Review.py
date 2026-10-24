"""Read-only PST50 quality audit. Encoded RGB statistics are not color calibration."""
import argparse
import csv
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps

AUTO = ['sha256','bytes','width','height','mode','png_bit_depth','orientation',
        'icc_present','png_srgb_intent','png_gamma','png_chromaticity_present',
        'camera_make','camera_model','iso','exposure_seconds','f_number',
        'red_mean','green_mean','blue_mean','encoded_luma_p01','encoded_luma_p50',
        'encoded_luma_p99','near_black_pixel_pct','near_white_pixel_pct',
        'channel_low_pct','channel_high_pct','laplacian_variance_proxy','dhash','exact_duplicate_of']
MANUAL = ['domain','capture_group','session_group','look_id','color_pipeline_verified',
          'source_target_alignment_verified','rights_evidence_reviewed','face_count',
          'skin_mask_reviewed','tone_target_approved','split']

def measure(im):
    # All images use the same analysis size; original samples preserved separately.
    small = im.convert('RGB').resize((256,256),Image.Resampling.BOX)
    x = np.asarray(small,dtype=np.float32)/255
    y = x @ np.array([.2126,.7152,.0722],dtype=np.float32)
    lap = -4*y[1:-1,1:-1]+y[:-2,1:-1]+y[2:,1:-1]+y[1:-1,:-2]+y[1:-1,2:]
    gray = np.asarray(im.convert('L').resize((9,8),Image.Resampling.BOX))
    bits = (gray[:,1:] > gray[:,:-1]).flatten()
    dh = f'{sum(int(v) << n for n,v in enumerate(bits)):016x}'
    return dict(red_mean=float(x[:,:,0].mean()),green_mean=float(x[:,:,1].mean()),blue_mean=float(x[:,:,2].mean()),
        **{f'encoded_luma_p{p:02d}':float(np.percentile(y,p)) for p in (1,50,99)},
        near_black_pixel_pct=float((x.max(2)<=1/255).mean()*100),
        near_white_pixel_pct=float((x.min(2)>=254/255).mean()*100),
        channel_low_pct=float((x<=1/255).mean()*100),channel_high_pct=float((x>=254/255).mean()*100),
        laplacian_variance_proxy=float(lap.var()),dhash=dh)

def run(root,out):
    root=root.resolve(); out=out.resolve()
    if out.exists() or out == root or root in out.parents:
        raise ValueError('Use a new output directory outside dataset.')
    provenance=json.loads((root/'provenance.json').read_text(encoding='utf-8-sig'))
    rows=[]; seen={}; previews=[]
    for asset in provenance['assets']:
        path=(root/asset['path']).resolve()
        if root not in path.parents:
            raise ValueError('Asset outside dataset')
        row={k:None for k in AUTO+MANUAL}
        row.update(path=asset['path'],role=asset['role'],case_id=asset['case_id'],flags=[],split='evaluation_only')
        raw=path.read_bytes(); row.update(sha256=hashlib.sha256(raw).hexdigest(),bytes=len(raw))
        if row['sha256'] != asset['sha256']:
            raise ValueError(f'Hash changed: {path}')
        row['exact_duplicate_of']=seen.get(row['sha256']); seen.setdefault(row['sha256'],row['path'])
        if raw[:8]==b'\x89PNG\r\n\x1a\n': row['png_bit_depth']=raw[24]
        try:
            with Image.open(path) as original:
                original.load(); info=original.info; exif=original.getexif()
                try: photo=exif.get_ifd(34665)
                except (KeyError,ValueError): photo={}
                row.update(width=original.width,height=original.height,mode=original.mode,
                    orientation=exif.get(274),icc_present=bool(info.get('icc_profile')),
                    png_srgb_intent=info.get('srgb'),png_gamma=info.get('gamma'),
                    png_chromaticity_present='chromaticity' in info,
                    camera_make=exif.get(271),camera_model=exif.get(272))
                for key,tag in [('iso',34855),('exposure_seconds',33434),('f_number',33437)]:
                    value=photo.get(tag)
                    if isinstance(value,(int,float)) or hasattr(value,'numerator'): row[key]=float(value)
                image=ImageOps.exif_transpose(original)
                row.update(measure(image))
                preview=image.convert('RGB'); preview.thumbnail((960,960)); previews.append((len(rows),preview.copy()))
                row['flags'] += ['color_pipeline_review','rights_review','manual_labels_pending']
                if row['png_bit_depth'] and row['png_bit_depth']>8: row['flags'].append('metrics_preview_8bit_not_native_precision')
                if row['channel_high_pct']>1: row['flags'].append('bright_channels_review')
                if row['channel_low_pct']>1: row['flags'].append('dark_channels_review')
        except (OSError,ValueError) as err:
            row['flags'].append('decode_failed'); row['decode_error']=str(err)
        case=next(c for c in provenance['cases'] if c['id']==row['case_id'])
        row['domain']=case.get('domain'); row['capture_group']=row['case_id']
        rows.append(row)
    # Similar appearance is a review cue only, including legitimate source/target pairs.
    candidates=[]
    for i,a in enumerate(rows):
        for b in rows[i+1:]:
            if a['dhash'] and b['dhash']:
                distance=(int(a['dhash'],16)^int(b['dhash'],16)).bit_count()
                if distance<=8: candidates.append(dict(a=a['path'],b=b['path'],hamming=distance,same_case=a['case_id']==b['case_id']))
    pairs=[]
    for case in provenance['cases']:
        a=next(r for r in rows if r['path']==case['source'])
        b=next(r for r in rows if r['path']==case['target'])
        pairs.append(dict(case_id=case['id'],source_target_dimensions_equal=(a['width'],a['height'])==(b['width'],b['height']),pixel_alignment_verified=False))
    out.mkdir(parents=True)
    (out/'previews').mkdir()
    for index,im in previews: im.save(out/'previews'/f'{index:02d}.jpg',quality=92)
    summary=dict(automatic_variables=len(AUTO),manual_variables=len(MANUAL),total_review_variables=len(AUTO+MANUAL),
        images=len(rows),decode_failed=sum('decode_failed' in r['flags'] for r in rows),
        missing_icc=sum(r['icc_present'] is False for r in rows),exact_duplicates=sum(r['exact_duplicate_of'] is not None for r in rows),
        training_ready=False,benchmark_ready=False,originals_modified=False,
        analysis='256x256 BOX preview RGB; encoded luma, not linear luminance; flags are not rejection',
        bit_depths=sorted({r['png_bit_depth'] for r in rows if r['png_bit_depth']}))
    report=dict(summary=summary,automatic_fields=AUTO,manual_fields=MANUAL,assets=rows,near_duplicate_candidates=candidates,pairs=pairs)
    (out/'review.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    with (out/'review.csv').open('w',newline='',encoding='utf-8-sig') as f:
        writer=csv.DictWriter(f,fieldnames=['path','role','case_id']+AUTO+MANUAL+['flags'],extrasaction='ignore'); writer.writeheader()
        for r in rows: writer.writerow({**r,'flags':';'.join(r['flags'])})
    print(json.dumps(summary,indent=2))

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--dataset',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
    a=p.parse_args(); run(a.dataset,a.output)
