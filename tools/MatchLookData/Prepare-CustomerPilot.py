"""Build local review assets and A/B protocol from an immutable inventory."""
import argparse
import hashlib
import html
import json
from collections import Counter
from pathlib import Path
from PIL import Image, ImageOps

def prepare(inventory, output, approved):
    data=json.loads(inventory.read_text(encoding='utf-8-sig'))
    source=Path(data['input_root']).resolve()
    if output.exists() or output.resolve().is_relative_to(source):
        raise ValueError('Use new output outside originals')
    output.mkdir(parents=True); (output/'previews').mkdir()
    assets=[]; cards=[]
    for entry in data['assets']:
        row=dict(entry); path=(source/row['path']).resolve()
        if not path.is_relative_to(source): raise ValueError('Invalid inventory path')
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest != row['sha256']: raise ValueError('Source changed: '+row['path'])
        row.update(asset_id=digest,rights_status='user_confirmed' if approved else 'pending',
            commercial_training_allowed=approved,rights_evidence='User confirmation in project chat, 2026-10-06' if approved else None,
            role='unassigned',look_family=[],session_group=None,subject_group=None,
            split='unassigned',leakage_group='customer-import-001-unresolved',training_ready=False)
        row['flags']=[f for f in row['flags'] if not (approved and f=='rights_review_required')]
        try:
            with Image.open(path) as im:
                # Reduced JPEG decode for gallery only; source remains untouched.
                im.draft('RGB',(960,960))
                preview=ImageOps.exif_transpose(im).convert('RGB'); preview.thumbnail((480,480))
                preview.save(output/'previews'/f'{digest}.jpg',quality=88)
            row['preview_status']='ready'
            cards.append(f'<article><img loading="lazy" src="previews/{digest}.jpg"><p>{html.escape(row["path"])}</p><p>{row["width"]} × {row["height"]} · {html.escape(str(row["camera_model"]))}</p></article>')
        except Exception as error:
            row.update(preview_status='failed',preview_error=str(error))
        assets.append(row)
    (output/'assets.json').write_text(json.dumps(assets,ensure_ascii=False,indent=2),encoding='utf-8')
    protocol=dict(version='pilot-ab-1',status='setup_not_executed',training_enabled=False,
        phase_a=dict(checkpoints=['E2E','SSL_updated_251111'],device='cpu',threads=4,
            tests=['checkpoint strict load and upstream source hash','identity LUT RGB axis check',
                'official demo parity on official samples','source=reference diagnostic (not exact identity requirement)',
                'fixed source with multiple references: neutral,warm,cool,monochrome',
                'different sources with same look reference','repeat determinism and finite output',
                'ICC/EXIF/range policy and two-stage LUT application'],
            gate='Explain weak transfer before fine-tuning; no visual-quality pass from strict loading alone'),
        phase_b=dict(assets_file='assets.json',triplets=[],
            required_triplet_fields=['source_asset_id','reference_asset_id','target_asset_id','look_id','reviewer_approved'],
            look_families=['natural','warm','cool','pastel','film','high_contrast','low_key','monochrome'],
            split_policy='Group all same-session/subject/derived images; no random split until groups reviewed',
            reference_policy='Reference can differ in scene; aligned source/target required for pixel metrics',
            gate='Reviewed roles, color pipeline, groups, targets and frozen holdout; consent alone is not readiness'))
    (output/'phase-ab.json').write_text(json.dumps(protocol,indent=2),encoding='utf-8')
    summary=dict(total=len(assets),preview_status=dict(Counter(r['preview_status'] for r in assets)),
        cameras=dict(Counter(str(r['camera_model']) for r in assets)),
        exact_duplicate_count=sum(bool(r['duplicate_of']) for r in assets),
        consent='user_confirmed' if approved else 'pending',training_ready=False,
        color_policy='Gallery EXIF-oriented, resized; no ICC transform. Original color pipeline pending review.')
    (output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    (output/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Customer pilot review</title><style>body{background:#222;color:#eee;font:14px system-ui;padding:24px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:12px}img{width:100%;height:250px;object-fit:contain}article{background:#333;padding:8px}</style><h1>Customer images · A/B pilot</h1><p>User-confirmed permission. Roles, looks and groups pending. No training performed.</p><main>'+''.join(cards)+'</main>',encoding='utf-8')
    print(json.dumps(summary,indent=2))

if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--inventory',required=True,type=Path)
    p.add_argument('--output',required=True,type=Path); p.add_argument('--user-approved-commercial-training',action='store_true')
    a=p.parse_args(); prepare(a.inventory,a.output,a.user_approved_commercial_training)
