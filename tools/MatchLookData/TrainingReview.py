"""Review gate: validates labels but never authorizes model training."""
import hashlib

def validate_review(review, dataset, dataset_bytes):
 errors=[];pending=[];expected={a['id'] for a in dataset['assets']};rows=review.get('assets',[])
 if review.get('dataset_sha256')!=hashlib.sha256(dataset_bytes).hexdigest():errors.append('dataset_hash_mismatch')
 if len(rows)!=len(expected) or {r.get('id') for r in rows}!=expected:errors.append('asset_set_mismatch')
 for r in rows:
  if r.get('decision') not in ('pending','reviewed','exclude'):errors.append('invalid_decision');continue
  if r['decision']=='exclude':continue
  if r['decision']!='reviewed':pending.append(r.get('id'));continue
  required=('base_look','subject_groups','duplicate_group')
  if any(not isinstance(r.get(k),str) or r[k].strip().lower() in ('','unknown','unreviewed') for k in required):pending.append(r.get('id'))
  if r.get('domain') not in ('portrait','landscape','mixed'):errors.append('invalid_domain')
 return dict(labels_complete=not errors and not pending,errors=sorted(set(errors)),pending_ids=pending,training_allowed=False,
             remaining=['target_provenance','split_assignment_and_leakage_checks','locked_holdout','trainer_validation','notify_user'])
