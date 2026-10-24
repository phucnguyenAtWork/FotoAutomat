"""Perturbation diagnostics; no identity inference or grading model changes."""
import os
from pathlib import Path
os.environ.setdefault('MPLCONFIGDIR',str(Path('.local/region-models/mpl').resolve()))
import argparse,json,time,hashlib
import numpy as np
import cv2
from PIL import Image
import mediapipe as mp
from RegionQuality import assess,face_geometry,iou,route_mask,POLICY,reject_overlapping_faces

def main():
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output
 if out.exists():raise ValueError('New output required')
 out.mkdir(parents=True)
 root=Path('.local/matchlook-data/region-audit-001');prior=json.loads((root/'report.json').read_text(encoding='utf-8'));reviewpath=Path('.local/region-review.json');review=json.loads(reviewpath.read_text(encoding='utf-8-sig'))
 if hashlib.sha256((root/'report.json').read_bytes()).hexdigest()!=review['report_sha256']:raise ValueError('Review hash mismatch')
 B=mp.tasks.BaseOptions;V=mp.tasks.vision;models=Path('.local/region-models')
 face=V.FaceLandmarker.create_from_options(V.FaceLandmarkerOptions(base_options=B(model_asset_path=str(models/'face_landmarker.task'),delegate=B.Delegate.CPU),num_faces=20,min_face_detection_confidence=.3,min_face_presence_confidence=.3))
 seg=V.ImageSegmenter.create_from_options(V.ImageSegmenterOptions(base_options=B(model_asset_path=str(models/'selfie_multiclass.tflite'),delegate=B.Delegate.CPU),output_confidence_masks=True,output_category_mask=True))
 inputs={c[role]['tag']:c[role] for c in prior['cases'] for role in ('source','reference')};rows=[];cards=[]
 for tag,record in inputs.items():
  rgb=np.array(Image.open(root/(tag+'-original.png')).convert('RGB'));h,w=rgb.shape[:2]
  gray=cv2.cvtColor(rgb,cv2.COLOR_RGB2GRAY)
  variants={'original':rgb,'repeat':rgb.copy(),'darker':np.round(rgb*.7).astype('uint8'),'brighter':np.clip(np.round(rgb*1.3),0,255).astype('uint8'),'grayscale':np.repeat(gray[...,None],3,-1),'half_size':cv2.resize(rgb,(w//2,h//2),interpolation=cv2.INTER_AREA)}
  baseline=None;variant_rows=[];panels=[];all_stable=None
  for name,pixels in variants.items():
   img=mp.Image(image_format=mp.ImageFormat.SRGB,data=np.ascontiguousarray(pixels));start=time.perf_counter();fr=face.detect(img);sr=seg.segment(img);elapsed=time.perf_counter()-start
   category=cv2.resize(sr.category_mask.numpy_view(),(w,h),interpolation=cv2.INTER_NEAREST);confidence=cv2.resize(sr.confidence_masks[3].numpy_view(),(w,h))
   masks=[];ovals=[];qualities=[]
   for lm in fr.face_landmarks:
    geometry,oval=face_geometry(lm,(h,w),V.FaceLandmarksConnections);mask,q=assess(geometry,category==3,confidence)
    # Confidence sweep reports threshold sensitivity, never lowers production gate silently.
    q['threshold_sweep']={str(v):assess(geometry,category==3,confidence,v)[1]['coverage'] for v in (.5,.7,.9)}
    masks.append(mask);ovals.append(oval);qualities.append(q)
   overlaps=reject_overlapping_faces(ovals,qualities)
   if baseline is None:baseline=(masks,ovals,qualities);all_stable=[True]*len(masks)
   # Geometry-only, one-to-one matching on same image; not recognition of identity.
   comparisons=[];used=set()
   for j,oval in enumerate(baseline[1]):
    choices=[(iou(oval,o) or 0,k) for k,o in enumerate(ovals) if k not in used]
    best=max(choices,default=(0,-1));k=best[1]
    if best[0]<.3:k=-1
    if k>=0:used.add(k)
    overlap=iou(baseline[0][j],masks[k]) if k>=0 else None
    stable=k>=0 and overlap is not None and overlap>=POLICY['min_consistency_iou'] and qualities[k]['eligible']
    if name!='original':all_stable[j]&=stable
    comparisons.append(dict(original_face=j+1,variant_face=k+1 if k>=0 else None,geometry_iou=best[0],skin_consistency_iou=overlap,stable=stable))
   canvas=rgb.copy()
   for mask,q in zip(masks,qualities):
    color=np.array([0,230,110] if q['eligible'] else [255,120,0]);canvas[mask]=(canvas[mask]*.4+color*.6).astype('uint8')
   file=tag+'-'+name+'.png';Image.fromarray(canvas).save(out/file)
   panels.append(f'<figure><figcaption>{name}: {len(masks)} faces / {sum(q["eligible"] for q in qualities)} eligible</figcaption><a href="{file}"><img loading="lazy" src="{file}"></a></figure>')
   variant_rows.append(dict(variant=name,faces=len(masks),seconds=elapsed,quality=qualities,overlapping_predictions=overlaps,matching=comparisons,unmatched_variant_faces=len(masks)-len(used)))
  safe=np.zeros((h,w),bool);routes=[]
  for mask,q,stable in zip(baseline[0],baseline[2],all_stable):
   accepted,decision=route_mask(mask,q,stable);safe|=accepted;routes.append(decision)
  Image.fromarray((safe*255).astype('uint8')).save(out/(tag+'-safe-mask.png'))
  rows.append(dict(tag=tag,variants=variant_rows,stable_faces=all_stable,routes=routes,no_faces_action='skip_regional_request_manual_review' if not baseline[0] else None))
  cards.append('<h2>'+tag+'</h2><section>'+''.join(panels)+'</section><p>'+str(routes)+'</p>');print(tag, 'stable',sum(all_stable),'/',len(all_stable),flush=True)
 face.close();seg.close()
 result=dict(policy=POLICY,mediapipe=mp.__version__,review_sha256=hashlib.sha256(reviewpath.read_bytes()).hexdigest(),model_sha256={f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in models.glob('*') if f.suffix in ('.task','.tflite')},images=rows,limitations=['Thresholds provisional','Consistency IoU is not ground truth accuracy','Greedy geometric correspondence may mismatch overlaps','Skin policy audit only; WPF grading not connected'])
 (out/'report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 (out/'index.html').write_text('<!doctype html><meta charset="utf-8"><style>body{background:#222;color:#eee;font:14px system-ui;padding:20px}section{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}figure{margin:0}img{width:100%;height:230px;object-fit:contain}</style><h1>Region robustness</h1><p>Green: passes spatial gate. Orange: rejected. Safe masks also require perturbation consistency. No automatic grading.</p>'+''.join(cards),encoding='utf-8')
if __name__=='__main__':main()
