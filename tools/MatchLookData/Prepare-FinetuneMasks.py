"""Quality-gated predicted skin masks for source pack, never identity recognition."""
import os,json,hashlib,time
from pathlib import Path
os.environ.setdefault('MPLCONFIGDIR',str(Path('.local/region-models/mpl').resolve()))
import cv2,numpy as np
from PIL import Image
import mediapipe as mp
from RegionQuality import assess,face_geometry,iou,route_mask,POLICY,reject_overlapping_faces

def main():
 root=Path('.local/matchlook-data/finetune-prep-2026-10-12-002');data=json.loads((root/'sources.json').read_text());out=root/'masks';out.mkdir(exist_ok=False);models=Path('.local/region-models');B=mp.tasks.BaseOptions;V=mp.tasks.vision
 face=V.FaceLandmarker.create_from_options(V.FaceLandmarkerOptions(base_options=B(model_asset_path=str(models/'face_landmarker.task'),delegate=B.Delegate.CPU),num_faces=20,min_face_detection_confidence=.3,min_face_presence_confidence=.3))
 seg=V.ImageSegmenter.create_from_options(V.ImageSegmenterOptions(base_options=B(model_asset_path=str(models/'selfie_multiclass.tflite'),delegate=B.Delegate.CPU),output_category_mask=True,output_confidence_masks=True));rows=[];start=time.perf_counter()
 for a in data['assets']:
  if a['split'] not in ('train','validation'):continue
  p=root/a['file']['path'];assert hashlib.sha256(p.read_bytes()).hexdigest()==a['file']['sha256'];rgb=np.array(Image.open(p).convert('RGB'));h,w=rgb.shape[:2];variants=[]
  for pixels in (rgb,np.round(rgb*.75).astype('uint8')):
   img=mp.Image(image_format=mp.ImageFormat.SRGB,data=np.ascontiguousarray(pixels));fr=face.detect(img);sr=seg.segment(img);category=cv2.resize(sr.category_mask.numpy_view(),(w,h),interpolation=cv2.INTER_NEAREST);conf=cv2.resize(sr.confidence_masks[3].numpy_view(),(w,h));masks=[];ovals=[];quality=[]
   for lm in fr.face_landmarks:
    geometry,oval=face_geometry(lm,(h,w),V.FaceLandmarksConnections);mask,q=assess(geometry,category==3,conf);masks.append(mask);ovals.append(oval);quality.append(q)
   reject_overlapping_faces(ovals,quality);variants.append((masks,ovals,quality))
  base,other=variants;used=set();safe=np.zeros((h,w),bool);records=[]
  for j,(m,o,q) in enumerate(zip(*base)):
   matches=[(iou(o,v) or 0,k) for k,v in enumerate(other[1]) if k not in used];best=max(matches,default=(0,-1));k=best[1]
   if best[0]<.3:k=-1
   if k>=0:used.add(k)
   consistency=iou(m,other[0][k]) if k>=0 else None;stable=k>=0 and consistency is not None and consistency>=POLICY['min_consistency_iou'] and other[2][k]['eligible'];accepted,decision=route_mask(m,q,stable);safe|=accepted;records.append(dict(quality=q,consistency_iou=consistency,decision=decision))
  mpth=out/(a['id']+'.png');Image.fromarray((safe*255).astype('uint8')).save(mpth)
  rows.append(dict(id=a['id'],split=a['split'],faces_detected=len(base[0]),eligible_faces=sum(r['decision']=='eligible_for_diagnostic_regional_use' for r in records),skin_pixels=int(safe.sum()),faces=records,file=dict(path=str(mpth.relative_to(root)),sha256=hashlib.sha256(mpth.read_bytes()).hexdigest())))
  if len(rows)%20==0:print('masks',len(rows),flush=True)
 face.close();seg.close();report=dict(policy=POLICY,images=rows,seconds=time.perf_counter()-start,models={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (models/'face_landmarker.task',models/'selfie_multiclass.tflite')},limitations=['Predicted not manually verified ground truth','Stability only original vs darkened; no identity accuracy','Missing/rejected face contributes zero skin loss; global loss remains'])
 (root/'mask-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(dict(images=len(rows),with_safe_skin=sum(r['skin_pixels']>0 for r in rows),eligible_faces=sum(r['eligible_faces'] for r in rows),seconds=report['seconds']),indent=2))
if __name__=='__main__':main()
