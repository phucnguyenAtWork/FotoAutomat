"""Rank dHash candidates with central-crop gradient structure. No identity inference."""
import json,hashlib
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw

def structure(a,b):
 def edges(im):
  x=np.asarray(im.convert('L').resize((128,128)),dtype=np.float32)/255
  x=x[16:112,24:104] # fixed central crop, not an inferred face/subject mask
  return np.concatenate([np.diff(x,axis=0).ravel(),np.diff(x,axis=1).ravel()])
 x,y=edges(a),edges(b);den=float(np.linalg.norm(x)*np.linalg.norm(y))
 return float(np.dot(x,y)/den) if den>1e-8 else None

def main():
 root=Path('.local/matchlook-data/phase-b-pilot-001');assets={a['id']:a for a in json.loads((root/'dataset.json').read_text())['assets']}
 original=Path('.local/matchlook-data/pretrain-audit-001');report=json.loads((original/'audit.json').read_text());out=Path('.local/matchlook-data/duplicate-structure-001');out.mkdir(exist_ok=False);images={}
 for pair in report['pairs']:
  for key in ('train','test'):
   aid=pair[key]
   if aid not in images:
    p=root/assets[aid]['standardized']['path'];assert hashlib.sha256(p.read_bytes()).hexdigest()==assets[aid]['standardized']['sha256'];images[aid]=Image.open(p).convert('RGB')
  pair['central_edge_cosine']=structure(images[pair['train']],images[pair['test']])
  pair['decision']='unresolved_no_automatic_release'
 pairs=sorted(report['pairs'],key=lambda p:p['central_edge_cosine'] if p['central_edge_cosine'] is not None else -1,reverse=True)
 for start in range(0,len(pairs),36):
  sheet=Image.new('RGB',(1200,12*130),'#222');draw=ImageDraw.Draw(sheet)
  for index,pair in enumerate(pairs[start:start+36]):
   x=(index%3)*400;y=(index//3)*130
   draw.text((x+4,y+2),f"{start+index+1}: edge={pair['central_edge_cosine']:.3f} hash={pair['distance']}",fill='white')
   for j,key in enumerate(('train','test')):
    im=images[pair[key]].copy();im.thumbnail((190,90));sheet.paste(im,(x+j*200,y+20));draw.text((x+j*200,y+112),assets[pair[key]]['original_name'],fill='white')
  sheet.save(out/f'sheet-{start//36+1}.jpg')
 (out/'report.json').write_text(json.dumps(dict(training_executed=False,automatic_release=False,method='cosine similarity of central grayscale finite-difference gradients; ranking only; not face recognition',pairs=pairs),indent=2))
 print(json.dumps(dict(pairs=len(pairs),sheets=3,output=str(out))))
if __name__=='__main__':main()
