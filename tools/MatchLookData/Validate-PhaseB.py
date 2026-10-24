"""Validate local artifacts and geometry; report readiness without changing approval."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image
from PhaseBData import validate_dataset

p=argparse.ArgumentParser(); p.add_argument('manifest',type=Path); a=p.parse_args()
data=json.loads(a.manifest.read_text(encoding='utf-8')); root=a.manifest.parent.resolve()
result=validate_dataset(data); checked={}; sizes={}
for artifact in [x['standardized'] for x in data['assets']]+[t[k] for t in data['triplets'] for k in ('source','reference','target')]:
    path=(root/artifact['path']).resolve()
    if not path.is_relative_to(root): raise ValueError('Artifact outside dataset')
    if path not in checked:
        checked[path]=hashlib.sha256(path.read_bytes()).hexdigest()
        with Image.open(path) as im:
            im.load(); sizes[path]=im.size
    if checked[path]!=artifact['sha256']: raise ValueError('Artifact hash mismatch: '+str(path))
for t in data['triplets']:
    if sizes[(root/t['source']['path']).resolve()] != sizes[(root/t['target']['path']).resolve()]:
        raise ValueError('Target geometry mismatch: '+t['id'])
result.update(verified_artifacts=len(checked),triplets=len(data['triplets']))
print(json.dumps(result,indent=2))
if not result['structure_valid']: raise SystemExit(1)
