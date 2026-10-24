"""Prepare color-managed customer probes and review UI; no training or approval."""
import argparse
import hashlib
import html
import io
import json
import re
from collections import Counter
from pathlib import Path
import numpy as np
from PIL import Image, ImageCms, ImageOps
from PhaseBData import LOOKS, render, validate_dataset

p=argparse.ArgumentParser(); p.add_argument('--inventory',type=Path,required=True)
p.add_argument('--pilot',type=Path,required=True); p.add_argument('--output',type=Path,required=True)
a=p.parse_args(); inv=json.loads(a.inventory.read_text(encoding='utf-8-sig'))
pilot=json.loads(a.pilot.read_text(encoding='utf-8-sig')); root=Path(inv['input_root']).resolve(); out=a.output.resolve()
if out.exists() or out.is_relative_to(root): raise ValueError('New output outside originals required')
out.mkdir(parents=True)
for folder in ('sources','targets','references'): (out/folder).mkdir()
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(relative,arr):
    Image.fromarray(arr).save(out/relative)
    return dict(path=relative,sha256=digest(out/relative))
assets=[]; previous=None; group=0
for i,row in enumerate(sorted(pilot,key=lambda r:r['path'])):
    path=(root/row['path']).resolve()
    if not path.is_relative_to(root) or digest(path)!=row['sha256']: raise ValueError('Invalid/changed source')
    number=int(re.search(r'(\d+)(?=\.[^.]+$)',path.name)[1])
    if previous is None or number-previous>20: group+=1
    previous=number
    with Image.open(path) as im:
        exif=im.getexif(); details=exif.get_ifd(34665); profile=im.info.get('icc_profile')
        stamp=details.get(36867); declared=details.get(40961)
        oriented=ImageOps.exif_transpose(im).convert('RGB')
        if profile:
            oriented=ImageCms.profileToProfile(oriented,ImageCms.ImageCmsProfile(io.BytesIO(profile)),ImageCms.createProfile('sRGB'),outputMode='RGB')
            policy='embedded_ICC_to_sRGB'
        elif declared==1: policy='EXIF_declared_sRGB'
        else: raise ValueError('Unknown color space: '+path.name)
        oriented.thumbnail((960,960),Image.Resampling.LANCZOS)
        arr=np.array(oriented); small=np.array(oriented.resize((32,32)).convert('L'))
        bits=(small[:,1:]>small[:,:-1]).reshape(-1)
        fingerprint=''.join('1' if x else '0' for x in bits)
    artifact=save('sources/'+row['sha256']+'.png',arr)
    assets.append(dict(id=row['sha256'],original_name=path.name,original_sha256=row['sha256'],
        standardized=artifact,width=arr.shape[1],height=arr.shape[0],color_policy=policy,
        capture_time=stamp,camera=row['camera_model'],rights_status=row['rights_status'],
        proposed_capture_group=f'filename-sequence-{group:02}',group_review='pending',
        leakage_group='customer-import-001-unresolved',split='diagnostic_only',source_role_review='pending',
        tone_labels=[],fingerprint=fingerprint))
    if (i+1)%40==0: print(f'Prepared {i+1}/{len(pilot)}',flush=True)
# Flag near copies for review only; never infer same identity or delete originals.
pairs=[]
for i,x in enumerate(assets):
    for y in assets[i+1:]:
        distance=sum(a!=b for a,b in zip(x['fingerprint'],y['fingerprint']))
        if distance<=35: pairs.append(dict(a=x['id'],b=y['id'],distance=distance))
for x in assets: del x['fingerprint']
groups={g:[x for x in assets if x['proposed_capture_group']==g] for g in sorted({x['proposed_capture_group'] for x in assets})}
selected=[]
for values in groups.values():
    selected.extend(values[j] for j in sorted(set(np.linspace(0,len(values)-1,min(4,len(values)),dtype=int))))
triplets=[]; cards=[]
for index,source in enumerate(selected):
    others=[r for r in selected if r['proposed_capture_group']!=source['proposed_capture_group']]
    reference=others[index%len(others)] if others else selected[(index+1)%len(selected)]
    with Image.open(out/source['standardized']['path']) as im: x=np.array(im)
    with Image.open(out/reference['standardized']['path']) as im: r=np.array(im)
    for recipe in LOOKS:
        key=f'case-{index+1:02}-{recipe["id"]}'
        target=save('targets/'+key+'.png',render(x,recipe)); ref=save('references/'+key+'.png',render(r,recipe))
        triplets.append(dict(id=key,source_id=source['id'],reference_source_id=reference['id'],
            source=source['standardized'],reference=ref,target=target,look_id=recipe['id'],
            renderer='encoded-srgb-probe-v1',target_kind='controlled_synthetic_not_human_grade',
            split='diagnostic_only',review_status='pending',target_geometry='same_as_standardized_source'))
        pictures=''.join(f'<figure><figcaption>{label}</figcaption><a href="{file}"><img loading="lazy" src="{file}"></a></figure>' for label,file in [('Source',source['standardized']['path']),('Reference',ref['path']),('Proposed target',target['path'])])
        cards.append(f'<article data-look="{recipe["id"]}"><h2>{key} · {html.escape(source["original_name"])}</h2><section>{pictures}</section><label>Đánh giá màu đích <select data-id="{key}"><option value="pending">Chưa duyệt</option><option value="accept">Phù hợp</option><option value="revise">Cần sửa</option><option value="reject">Loại</option></select></label></article>')
data=dict(version='phase-b-1',assets=assets,triplets=triplets,recipes=LOOKS,holdout_locked=False,
    renderer_scope='Controlled transforms of encoded sRGB, not named camera emulation or approved human targets',
    input_manifest_sha256=digest(a.pilot),near_duplicate_candidates=pairs)
validation=validate_dataset(data); data['validation']=validation
(out/'dataset.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
manifest_hash=digest(out/'dataset.json')
summary=dict(assets=len(assets),source_probes=len(selected),triplets=len(triplets),looks=len(LOOKS),
    proposed_groups={k:len(v) for k,v in groups.items()},near_duplicate_pairs=len(pairs),**validation)
(out/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
header='<!doctype html><meta charset="utf-8"><title>Phase B · Look review</title><style>body{font:15px system-ui;background:#202020;color:#eee;margin:24px}header{position:sticky;top:0;background:#202020;padding:12px;z-index:1}section{display:flex;gap:12px}figure{flex:1;min-width:0;margin:0}img{width:100%;height:270px;object-fit:contain}article{padding:20px 0;border-bottom:1px solid #555}button,select{padding:10px;font:inherit}a{color:#8cdddf}</style><header><h1>Phase B · Duyệt 8 look thử nghiệm</h1><p>Source → Reference khác ảnh → Target tạo bằng công thức. Chưa train, chưa phải màu chuẩn được duyệt.</p><select id="filter"><option value="all">Tất cả look</option>'+''.join(f'<option>{r["id"]}</option>' for r in LOOKS)+'</select> <button id="export">Xuất đánh giá JSON</button><span id="status"></span></header>'
script='''<script>
const hash=HASH, key='phase-b-review-'+hash; let decisions={};
try { decisions=JSON.parse(localStorage.getItem(key)||'{}'); } catch(e) {}
document.querySelectorAll('select[data-id]').forEach(s=>{s.value=decisions[s.dataset.id]||'pending';s.onchange=()=>{decisions[s.dataset.id]=s.value;try{localStorage.setItem(key,JSON.stringify(decisions));document.querySelector('#status').textContent=' Đã lưu trên trình duyệt; xuất JSON để gửi.'}catch(e){document.querySelector('#status').textContent=' Hãy xuất JSON để giữ đánh giá.'}}});
document.querySelector('#filter').onchange=e=>document.querySelectorAll('article').forEach(a=>a.hidden=e.target.value!=='all'&&a.dataset.look!==e.target.value);
document.querySelector('#export').onclick=()=>{const blob=new Blob([JSON.stringify({manifest_sha256:hash,decisions,training_approval:false},null,2)],{type:'application/json'});const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download='phase-b-review.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000)};
</script>'''.replace('HASH',json.dumps(manifest_hash))
(out/'index.html').write_text(header+''.join(cards)+script,encoding='utf-8')
print(json.dumps(summary,indent=2),flush=True)
