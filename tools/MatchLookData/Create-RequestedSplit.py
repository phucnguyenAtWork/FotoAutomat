"""Create explicit requested image-level 85/15 allocation; report session leakage."""
import hashlib,json,random
from pathlib import Path
root=Path('.local/matchlook-data/phase-b-pilot-001');raw=(root/'dataset.json').read_bytes();data=json.loads(raw)
out=Path('.local/matchlook-data/split-85-15-001');out.mkdir(parents=True,exist_ok=False)
assets=sorted(data['assets'],key=lambda a:a['id']);random.Random(20261009).shuffle(assets)
n_test=round(len(assets)*.15);test_ids={a['id'] for a in assets[:n_test]}
rows=[]
for a in assets:
 p=root/a['standardized']['path']
 assert hashlib.sha256(p.read_bytes()).hexdigest()==a['standardized']['sha256']
 rows.append(dict(id=a['id'],name=a['original_name'],session=a['capture_group'],split='test' if a['id'] in test_ids else 'train',sha256=a['standardized']['sha256'],role='graded_reference_or_self_supervised_input_not_paired_ground_truth'))
train={r['id'] for r in rows if r['split']=='train'};test={r['id'] for r in rows if r['split']=='test'}
assert not train&test and len(train|test)==len(data['assets'])
sessions={s:{k:sum(r['session']==s and r['split']==k for r in rows) for k in ('train','test')} for s in sorted({r['session'] for r in rows})}
report=dict(requested_train_fraction=.85,seed=20261009,method='seeded_image_level_allocation',dataset_sha256=hashlib.sha256(raw).hexdigest(),train_count=len(train),test_count=len(test),train_fraction=len(train)/len(rows),sessions=sessions,training_executed=False,production_training_ready=False,test_is_fresh=False,limitations=['Session overlap across train/test; near-duplicate and subject overlap not resolved','Collection previously used for diagnostic development; not an untouched final benchmark','No validation allocation yet; validation must come from train, never test','No paired ground truth inferred from graded JPEGs'],assets=rows)
(out/'manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for split in ('train','test'):(out/(split+'-ids.json')).write_text(json.dumps(sorted(r['id'] for r in rows if r['split']==split),indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='assets'},indent=2))
