"""Controlled look probes and readiness checks, independent of CanonCGT."""
import numpy as np

LOOKS = [
    dict(id='neutral', gains=[1,1,1], exposure=0, contrast=1, saturation=1, lift=0, monochrome=False),
    dict(id='warm', gains=[1.08,1,.9], exposure=0, contrast=1, saturation=1, lift=0, monochrome=False),
    dict(id='cool', gains=[.9,1,1.08], exposure=0, contrast=1, saturation=1, lift=0, monochrome=False),
    dict(id='pastel', gains=[1.02,1,1.01], exposure=.1, contrast=.82, saturation=.75, lift=.02, monochrome=False),
    dict(id='film_matte', gains=[1.03,1,.96], exposure=-.1, contrast=.9, saturation=.8, lift=.035, monochrome=False),
    dict(id='high_contrast', gains=[1,1,1], exposure=0, contrast=1.22, saturation=1, lift=0, monochrome=False),
    dict(id='low_key', gains=[1,1,1], exposure=-.65, contrast=1.08, saturation=.9, lift=0, monochrome=False),
    dict(id='monochrome', gains=[1,1,1], exposure=0, contrast=1.08, saturation=0, lift=0, monochrome=True),
]

def render(rgb, recipe):
    """Explicit encoded-sRGB diagnostic transform; not a camera/Lightroom emulation."""
    x=np.asarray(rgb,dtype=np.float32)/255
    x=x*np.asarray(recipe['gains'],dtype=np.float32)*(2**recipe['exposure'])
    y=(x*np.array([.2126,.7152,.0722],dtype=np.float32)).sum(-1,keepdims=True)
    x=y+(x-y)*recipe['saturation']
    if recipe['monochrome']: x=np.repeat(y,3,axis=-1)
    x=(x-.5)*recipe['contrast']+.5+recipe['lift']
    return (np.clip(x,0,1)*255).round().astype('uint8')

def validate_dataset(data):
    """Never promote pending records to training ready; validate all triplet parents."""
    assets={a['id']:a for a in data['assets']}
    errors=[]
    if len(assets)!=len(data['assets']): errors.append('duplicate_asset_ids')
    ids=set()
    for triplet in data['triplets']:
        if triplet['id'] in ids: errors.append('duplicate_triplet_ids')
        ids.add(triplet['id'])
        parents=[assets.get(triplet[k]) for k in ('source_id','reference_source_id')]
        if any(p is None for p in parents):
            errors.append('missing_parent'); continue
        if triplet['source_id']==triplet['reference_source_id']: errors.append('same_reference_content')
        if len({p['split'] for p in parents}|{triplet['split']})!=1: errors.append('cross_split_triplet')
    group_splits={}
    for asset in data['assets']:
        group_splits.setdefault(asset['leakage_group'],set()).add(asset['split'])
    if any(len(s)>1 for s in group_splits.values()): errors.append('leakage_group_crosses_splits')
    blockers=[]
    if not data['triplets']: blockers.append('no_triplets')
    if any(a['group_review']!='approved' for a in data['assets']): blockers.append('groups_pending')
    if any(a['source_role_review']!='approved' for a in data['assets']): blockers.append('source_roles_pending')
    if any(a['rights_status']!='user_confirmed' for a in data['assets']): blockers.append('rights_pending')
    if any(a['split'] not in ('train','validation','test') for a in data['assets']): blockers.append('splits_unassigned')
    if any(t['review_status']!='approved' for t in data['triplets']): blockers.append('target_look_review_pending')
    if not data.get('holdout_locked'): blockers.append('holdout_not_locked')
    return dict(structure_valid=not errors,errors=sorted(set(errors)),blockers=blockers,
                training_ready=not errors and not blockers)
