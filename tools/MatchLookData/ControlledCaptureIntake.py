"""Read-only controlled-capture intake. Does not decode RAW, calibrate or train."""
import argparse,hashlib,json,math
from pathlib import Path
from PIL import Image

CONFIG_KEYS=('camera_model','lens','firmware','iso','shutter_seconds','aperture','wb_mode','wb_value','jpeg_style','color_space','flash_mode','flash_power','continuous_light','light_positions','subject_positions')

def audit(manifest,root):
 root=Path(root).resolve();errors=[];blockers=[];files=[];seen_ids=set();group_partitions={};hash_partitions={};coverage={}
 if manifest.get('schema')!='fotoautomat-controlled-capture-v1':errors.append('schema_invalid')
 configs=manifest.get('configs',[]);captures=manifest.get('captures',[])
 if not isinstance(configs,list) or not isinstance(captures,list):raise ValueError('configs/captures must be arrays')
 configmap={}
 for cfg in configs:
  cid=cfg.get('id')
  if not isinstance(cid,str) or not cid.strip() or cid in configmap:errors.append('config_id_invalid_or_duplicate');continue
  configmap[cid]=cfg
  missing=[k for k in CONFIG_KEYS if cfg.get(k) is None or cfg.get(k)=='' or cfg.get(k)==[]]
  if missing:blockers.append(f'{cid}:missing_config:'+','.join(missing))
  if cfg.get('exposure_mode')!='M' or cfg.get('auto_iso') is not False or cfg.get('wb_mode') not in ('custom','kelvin') or cfg.get('flash_mode')!='manual':blockers.append(f'{cid}:capture_not_confirmed_locked')
  for k in ('iso','shutter_seconds','aperture'):
   v=cfg.get(k)
   if type(v) not in (int,float) or not math.isfinite(v) or v<=0:blockers.append(f'{cid}:invalid_'+k)
 if not configs:blockers.append('no_configs')
 if not captures:blockers.append('no_captures')
 for c in captures:
  cid=c.get('id');cfg=c.get('config_id');partition=c.get('partition');session=c.get('session_id');burst=c.get('burst_id')
  if not isinstance(cid,str) or not cid.strip() or cid in seen_ids:errors.append('capture_id_invalid_or_duplicate')
  seen_ids.add(cid)
  if cfg not in configmap:errors.append(f'{cid}:unknown_config')
  if partition not in ('calibration','validation'):errors.append(f'{cid}:invalid_partition')
  if not session or not burst:errors.append(f'{cid}:missing_session_or_burst')
  else:
   for kind,gid in (('session',session),('burst',burst)):
    group_partitions.setdefault((kind,gid),set()).add(partition)
  subject=c.get('subject')
  if subject not in ('chart','one_person','four_people'):errors.append(f'{cid}:invalid_subject')
  if c.get('rights_confirmed') is not True:blockers.append(f'{cid}:rights_pending')
  if c.get('continuous_on_during_capture') is not True:blockers.append(f'{cid}:combined_lighting_unconfirmed')
  if c.get('pairing_verified') is not True:blockers.append(f'{cid}:raw_jpeg_liveview_pairing_pending')
  if not c.get('position_id'):blockers.append(f'{cid}:position_missing')
  complete=True
  for role in ('raw','jpeg','liveview'):
   spec=c.get(role)
   if not isinstance(spec,dict) or not spec.get('path') or not spec.get('sha256'):
    blockers.append(f'{cid}:{role}_missing_path_or_hash');complete=False;continue
   relative=Path(spec['path']);path=(root/relative).resolve()
   if relative.is_absolute() or not path.is_relative_to(root):errors.append(f'{cid}:{role}_path_outside_root');complete=False;continue
   if not path.is_file():errors.append(f'{cid}:{role}_missing_file');complete=False;continue
   h=hashlib.sha256()
   with path.open('rb') as stream:
    for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
   digest=h.hexdigest()
   if digest!=spec['sha256']:errors.append(f'{cid}:{role}_hash_mismatch');complete=False
   hash_partitions.setdefault(digest,set()).add(partition)
   row=dict(capture_id=cid,role=role,path=str(relative),sha256=digest,bytes=path.stat().st_size)
   if role=='raw':
    row['decode_verified']=False
    if path.suffix.lower() not in ('.raf','.cr3','.cr2','.dng'):errors.append(f'{cid}:unsupported_raw_extension');complete=False
   else:
    try:
     with Image.open(path) as im:
      im.load();row.update(width=im.width,height=im.height,format=im.format,icc_present=bool(im.info.get('icc_profile')))
      if role=='jpeg' and im.format!='JPEG':errors.append(f'{cid}:jpeg_not_jpeg');complete=False
    except Exception as ex:errors.append(f'{cid}:{role}_decode_error:{type(ex).__name__}');complete=False
   files.append(row)
  if complete:coverage.setdefault((cfg,partition,subject,c.get('position_id')),set()).add(cid)
 for (kind,gid),parts in group_partitions.items():
  if len(parts)>1:errors.append(f'{kind}:{gid}:cross_partition')
 if any(len(v)>1 for v in hash_partitions.values()):errors.append('identical_file_cross_partition')
 for cfg in configmap:
  positions=configmap[cfg].get('subject_positions') or []
  if not isinstance(positions,list):errors.append(f'{cfg}:positions_not_array');continue
  for partition in ('calibration','validation'):
   for subject in ('chart','one_person','four_people'):
    for position in (positions if subject=='chart' else ['center']):
     if len(coverage.get((cfg,partition,subject,position),set()))<3:blockers.append(f'{cfg}:{partition}:{subject}:{position}:need_3_captures')
 return dict(schema='fotoautomat-capture-intake-report-v1',structure_valid=not errors,intake_complete=bool(captures) and not errors and not blockers,errors=sorted(set(errors)),blockers=sorted(set(blockers)),files=files,capture_count=len(captures),training_ready=False,profile_ready=False,limitations=['RAW only hashed; extension checked, decoder not run','Manual pairing and capture settings declarations not hardware verified','Exact hash isolation only; near-duplicates not detected','Completeness is not chart/skin color accuracy or training approval'])

def main():
 ap=argparse.ArgumentParser();ap.add_argument('manifest',type=Path);ap.add_argument('--root',required=True,type=Path);ap.add_argument('--report',required=True,type=Path);args=ap.parse_args()
 data=json.loads(args.manifest.read_text(encoding='utf-8-sig'));result=audit(data,args.root)
 if args.report.exists():raise FileExistsError('Report already exists; choose a new path')
 args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k!='files'},ensure_ascii=False,indent=2))
 raise SystemExit(0 if result['intake_complete'] else 2)
if __name__=='__main__':main()
