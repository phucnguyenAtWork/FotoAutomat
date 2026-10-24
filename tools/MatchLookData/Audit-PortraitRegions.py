"""CPU region audit. Predicted masks are review candidates, never ground truth."""
import os
from pathlib import Path
os.environ.setdefault('MPLCONFIGDIR',str(Path('.local/region-models/mpl').resolve()))
import argparse,json,hashlib,time,html
import numpy as np
import cv2
from PIL import Image,ImageDraw
import mediapipe as mp

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def load(p):
 with Image.open(p) as im:return np.array(im.convert('RGB'))
def metrics(x,y,mask):
 if not mask.any():return dict(pixels=0,status='no_valid_skin_roi')
 a=x.astype(np.float32)/255;b=y.astype(np.float32)/255
 la=cv2.cvtColor(a,cv2.COLOR_RGB2Lab);lb=cv2.cvtColor(b,cv2.COLOR_RGB2Lab)
 return dict(pixels=int(mask.sum()),mae_255=float(np.abs(x.astype(float)-y)[mask].mean()),deltaE76_mean=float(np.linalg.norm(la-lb,axis=-1)[mask].mean()),delta_L_median=float(np.median((la-lb)[...,0][mask])),near_ceiling_any_channel=float((x.max(-1)[mask]>=250).mean()),new_near_ceiling_vs_target=float(((x.max(-1)>=250)&(y.max(-1)<250))[mask].mean()))

def main():
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output
 if out.exists():raise ValueError('Use new output')
 out.mkdir(parents=True)
 root=Path('.local/matchlook-data/phase-b-pilot-001');baseline=Path('.local/matchlook-data/portrait-model-review-001');models=Path('.local/region-models')
 data=json.loads((root/'dataset.json').read_text());report=json.loads((baseline/'report.json').read_text())
 looks=['neutral','warm','low_key','cool','pastel','high_contrast','monochrome','film_matte','cool','neutral','pastel','film_matte']
 cases=[next(t for t in data['triplets'] if t['id']==f'case-{i+1:02}-{look}') for i,look in enumerate(looks)]
 B=mp.tasks.BaseOptions;V=mp.tasks.vision;C=V.FaceLandmarksConnections
 face=V.FaceLandmarker.create_from_options(V.FaceLandmarkerOptions(base_options=B(model_asset_path=str(models/'face_landmarker.task'),delegate=B.Delegate.CPU),num_faces=20,min_face_detection_confidence=.3,min_face_presence_confidence=.3))
 seg=V.ImageSegmenter.create_from_options(V.ImageSegmenterOptions(base_options=B(model_asset_path=str(models/'selfie_multiclass.tflite'),delegate=B.Delegate.CPU),output_category_mask=True,output_confidence_masks=True))
 palette=np.array([[70,100,180],[180,80,220],[255,170,70],[50,230,130],[90,180,230],[230,90,110]],dtype=np.uint8)
 cache={};rows=[];cards=[]
 def analyze(path,tag):
  key=sha(path)
  if key in cache:return cache[key]
  rgb=load(path);h,w=rgb.shape[:2];img=mp.Image(image_format=mp.ImageFormat.SRGB,data=rgb)
  # First call warms kernels; timings use three subsequent calls, include no file I/O.
  face.detect(img);seg.segment(img);times=[]
  for _ in range(3):
   start=time.perf_counter();fr=face.detect(img);sr=seg.segment(img);times.append(time.perf_counter()-start)
  category=sr.category_mask.numpy_view().copy();conf=sr.confidence_masks[3].numpy_view().copy()
  if category.shape!=(h,w):category=cv2.resize(category,(w,h),interpolation=cv2.INTER_NEAREST);conf=cv2.resize(conf,(w,h))
  if category.max()>5:raise ValueError('Unexpected label schema')
  masks=[];overlay=Image.fromarray(((rgb.astype(float)*.65+palette[category]*.35)).astype('uint8'));draw=ImageDraw.Draw(overlay)
  for idx,lm in enumerate(fr.face_landmarks):
   pts=np.array([[v.x*w,v.y*h] for v in lm],dtype=np.int32)
   def hull(connections):return cv2.convexHull(pts[sorted({v for c in connections for v in (c.start,c.end)})])
   m=np.zeros((h,w),np.uint8);cv2.fillConvexPoly(m,hull(C.FACE_LANDMARKS_FACE_OVAL),1)
   for con in (C.FACE_LANDMARKS_LEFT_EYE,C.FACE_LANDMARKS_RIGHT_EYE,C.FACE_LANDMARKS_LIPS,C.FACE_LANDMARKS_LEFT_EYEBROW,C.FACE_LANDMARKS_RIGHT_EYEBROW):cv2.fillConvexPoly(m,hull(con),0)
   # Conservative erosion removes landmark boundary leakage; intersect learned face-skin.
   m=cv2.erode(m,np.ones((3,3),np.uint8)).astype(bool)&(category==3)&(conf>=.7);masks.append(m)
   oval=hull(C.FACE_LANDMARKS_FACE_OVAL).reshape(-1,2);draw.line([tuple(v) for v in oval]+[tuple(oval[0])],fill='white',width=2);draw.text(tuple(pts[10]),str(idx+1),fill='yellow')
   Image.fromarray((m*255).astype('uint8')).save(out/f'{tag}-face-{idx+1}-skin.png')
  union=np.logical_or.reduce(masks) if masks else np.zeros((h,w),bool)
  skinview=rgb.copy();skinview[union]=(rgb[union]*.4+np.array([0,255,110])*.6).astype('uint8')
  Image.fromarray(rgb).save(out/f'{tag}-original.png');overlay.save(out/f'{tag}-regions.png');Image.fromarray(skinview).save(out/f'{tag}-skin.png');Image.fromarray(category).save(out/f'{tag}-labels.png')
  row=dict(tag=tag,sha256=key,faces=len(masks),valid_skin_faces=sum(int(m.sum())>=50 for m in masks),skin_pixels=int(union.sum()),seconds_median=float(np.median(times)),seconds=times,mask_review='pending',expected_faces=None,class_fractions={str(i):float((category==i).mean()) for i in range(6)})
  cache[key]=(row,masks,union);return cache[key]
 for t in cases:
  cid=t['id']
  for k in ('source','reference','target'):
   if sha(root/t[k]['path'])!=t[k]['sha256']:raise ValueError('Dataset changed')
  sr,masks,union=analyze(root/t['source']['path'],cid+'-source');rr,_,_=analyze(root/t['reference']['path'],cid+'-reference')
  target=load(root/t['target']['path']);prior=next(c for c in report['cases'] if c['id']==cid);measure={}
  for label in ('SSL','E2E'):
   x=load(baseline/prior['models'][label]['file'])
   if x.shape!=target.shape:raise ValueError('Geometry mismatch')
   measure[label]=dict(all_predicted_skin=metrics(x,target,union),per_face=[metrics(x,target,m) for m in masks])
  rows.append(dict(id=cid,source=sr,reference=rr,metrics=measure,target_reference_review='pending: shared incremental recipe, different existing base presets'))
  figures=''.join(f'<figure><figcaption>{role} / {kind}</figcaption><a href="{rec["tag"]}-{kind}.png"><img loading="lazy" src="{rec["tag"]}-{kind}.png"></a></figure>' for role,rec in [('Source',sr),('Reference',rr)] for kind in ('original','regions','skin'))
  cards.append(f'<article><h2>{cid} · faces {sr["faces"]} / {rr["faces"]}</h2><section>{figures}</section><label>Mask <select data-key="{cid}-mask"><option>Chưa duyệt</option><option>Đúng</option><option>Thiếu mặt/da</option><option>Lẫn tóc/mắt/môi/nền</option></select></label><input data-key="{cid}-note" placeholder="Ghi chú và số mặt thật source/reference"></article>')
  print(cid,sr['faces'],rr['faces'],flush=True)
 face.close();seg.close()
 result=dict(mediapipe=mp.__version__,device='CPU',models={f.name:sha(f) for f in models.glob('*') if f.suffix in ('.task','.tflite')},dataset_sha256=sha(root/'dataset.json'),cases=rows,limitations=['Masks predicted, no ground truth accuracy/IoU','DeltaE76 not DeltaE00','No image grading or training performed','Near ceiling is heuristic, not proof of lost detail','No-face landscape validation still required'])
 (out/'report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 key=sha(out/'report.json');js='''<script>const key='regions-'+HASH;let d={};try{d=JSON.parse(localStorage.getItem(key)||'{}')}catch(e){}document.querySelectorAll('[data-key]').forEach(e=>{if(d[e.dataset.key])e.value=d[e.dataset.key];e.onchange=()=>{d[e.dataset.key]=e.value;try{localStorage.setItem(key,JSON.stringify(d))}catch(e){}}});document.querySelector('button').onclick=()=>{let u=URL.createObjectURL(new Blob([JSON.stringify({report_sha256:HASH,decisions:d},null,2)],{type:'application/json'}));let a=document.createElement('a');a.href=u;a.download='region-review.json';a.click();setTimeout(()=>URL.revokeObjectURL(u),1000)}</script>'''.replace('HASH',json.dumps(key))
 (out/'index.html').write_text('<!doctype html><meta charset="utf-8"><style>body{background:#222;color:#eee;font:15px system-ui;padding:20px}section{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}figure{margin:0}img{width:100%;height:280px;object-fit:contain}article{margin:30px 0}input,select,button{padding:10px}input{width:400px}</style><h1>Region audit · 12 cases</h1><p>Regions: xanh dương=nền; tím=tóc; cam=da cơ thể; xanh lá=da mặt; xanh nhạt=áo; đỏ=phụ kiện. Skin: vùng xanh lá dùng đo màu; đã loại mắt/môi bằng landmarks. Mask dự đoán, chưa duyệt.</p><button>Xuất đánh giá mask</button>'+''.join(cards)+js,encoding='utf-8')
 # Compact contact sheet, all sources, no repeated reference panels.
 sheet=Image.new('RGB',(1080,12*210),'#222');dr=ImageDraw.Draw(sheet)
 for i,row in enumerate(rows):
  for j,k in enumerate(('original','regions','skin')):
   with Image.open(out/(row['source']['tag']+'-'+k+'.png')) as im:
    im.thumbnail((355,183));sheet.paste(im,(j*360,i*210+22));dr.text((j*360+3,i*210+3),row['id']+' / '+k,fill='white')
 sheet.save(out/'overview.jpg',quality=90)
 print('Saved',out)
if __name__=='__main__':main()
