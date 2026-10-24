"""Build local review cards; exported labels are proposals, never training approval."""
import argparse,hashlib,html,json
from pathlib import Path
from PIL import Image

def main():
 p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);a=p.parse_args()
 root=Path('.local/matchlook-data/phase-b-pilot-001');raw=(root/'dataset.json').read_bytes();data=json.loads(raw)
 a.output.mkdir(parents=True,exist_ok=False);(a.output/'thumbs').mkdir()
 records=[];cards=[]
 for asset in data['assets']:
  source=root/asset['standardized']['path']
  if hashlib.sha256(source.read_bytes()).hexdigest()!=asset['standardized']['sha256']:raise ValueError('Source changed')
  im=Image.open(source).convert('RGB');im.thumbnail((240,180));im.save(a.output/'thumbs'/(asset['id']+'.jpg'))
  records.append(dict(id=asset['id'],name=asset['original_name'],session=asset['capture_group'],base_look='',subject_groups='',duplicate_group='',domain='unreviewed',decision='pending',notes=''))
  i=len(records)-1
  cards.append(f'''<article data-i="{i}"><img loading="lazy" src="thumbs/{asset['id']}.jpg"><b>{html.escape(asset['original_name'])}</b><small>{html.escape(asset['capture_group'])}</small>
<label>Base look / preset<input data-key="base_look" placeholder="Unknown nếu chưa rõ"></label>
<label>Nhóm người (mã tự đặt, cách nhau dấu phẩy)<input data-key="subject_groups" placeholder="Ví dụ P01,P02; none nếu không có người"></label>
<label>Nhóm ảnh gần trùng<input data-key="duplicate_group" placeholder="D01; unique nếu đã kiểm tra"></label>
<label>Loại ảnh<select data-key="domain"><option>unreviewed</option><option>portrait</option><option>landscape</option><option>mixed</option></select></label>
<label>Quyết định<select data-key="decision"><option>pending</option><option>reviewed</option><option>exclude</option></select></label>
<label>Ghi chú<input data-key="notes"></label></article>''')
 payload=dict(schema='matchlook-review-v1',dataset_sha256=hashlib.sha256(raw).hexdigest(),training_allowed=False,assets=records)
 (a.output/'review-template.json').write_text(json.dumps(payload,indent=2),encoding='utf-8')
 page='''<!doctype html><meta charset="utf-8"><title>Data review</title><style>body{background:#202326;color:#eee;font:14px system-ui;margin:24px}header{position:sticky;top:0;background:#202326;padding:12px;z-index:1}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(255px,1fr));gap:12px}article{background:#30363a;padding:12px}img{width:100%;height:180px;object-fit:contain}b,small,label{display:block;margin:6px 0}input,select{box-sizing:border-box;width:100%;padding:7px}button{padding:10px;margin-right:12px}</style><header><h1>Duyệt dữ liệu trước train</h1><p>Gán cùng mã người khi cùng người xuất hiện ở ảnh khác. Base look chưa biết: để trống. Reviewed chỉ xác nhận nhãn; chưa duyệt target hay cho phép train.</p><button id="save">Export review JSON</button><button id="restore">Import review JSON</button><input id="file" type="file" accept=".json" hidden><span id="status"></span></header><main>'''+''.join(cards)+'''</main><script>
const data=PAYLOAD;
function status(){document.querySelector('#status').textContent=data.assets.filter(x=>x.decision!=='pending').length+'/'+data.assets.length+' reviewed/excluded';}
document.querySelector('main').addEventListener('input',e=>{const key=e.target.dataset.key;if(key){data.assets[+e.target.closest('article').dataset.i][key]=e.target.value;status();}});
document.querySelector('#save').onclick=()=>{const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:'application/json'}));a.download='data-review.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);};
document.querySelector('#restore').onclick=()=>document.querySelector('#file').click();
document.querySelector('#file').onchange=async e=>{try{const v=JSON.parse(await e.target.files[0].text());if(v.dataset_sha256!==data.dataset_sha256||v.assets.length!==data.assets.length||v.assets.some((x,i)=>x.id!==data.assets[i].id))throw Error('Dataset không khớp');for(let i=0;i<data.assets.length;i++)for(const key of ['base_look','subject_groups','duplicate_group','domain','decision','notes']){if(typeof v.assets[i][key]!=='string')throw Error('Nhãn không hợp lệ');}for(let i=0;i<data.assets.length;i++)for(const key of ['base_look','subject_groups','duplicate_group','domain','decision','notes'])data.assets[i][key]=v.assets[i][key];document.querySelectorAll('article').forEach(a=>a.querySelectorAll('[data-key]').forEach(el=>el.value=data.assets[+a.dataset.i][el.dataset.key]));status();}catch(err){alert(err.message);}};status();
</script>'''.replace('PAYLOAD',json.dumps(payload).replace('<','\\u003c'))
 (a.output/'index.html').write_text(page,encoding='utf-8');print(json.dumps(dict(assets=len(records),output=str(a.output),training_executed=False)))
if __name__=='__main__':main()
