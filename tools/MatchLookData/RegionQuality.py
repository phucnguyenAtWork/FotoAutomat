"""Inspectable region quality gates; provisional thresholds, not calibrated accuracy."""
import numpy as np
import cv2

POLICY = dict(min_pixels=100, min_coverage=.35, min_zone_coverage=.15,
              min_supported_zones=3, min_consistency_iou=.65)

def assess(geometry, category_skin, confidence, threshold=.7):
    """Separate geometric support, semantic class and confidence filtering."""
    geometry=np.asarray(geometry,bool)
    semantic=geometry & np.asarray(category_skin,bool)
    accepted=semantic & (confidence>=threshold)
    total=int(geometry.sum()); semantic_count=int(semantic.sum()); count=int(accepted.sum())
    zones=[]
    yy,xx=np.where(geometry)
    if total:
        cx=(xx.min()+xx.max())/2;cy=(yy.min()+yy.max())/2
        y,x=np.indices(geometry.shape)
        for side_y in (y<=cy,y>cy):
            for side_x in (x<=cx,x>cx):
                zone=geometry & side_y & side_x
                zones.append(float((accepted & zone).sum()/max(1,zone.sum())))
    coverage=count/max(1,total);reasons=[]
    if total<POLICY['min_pixels']:reasons.append('insufficient_geometry')
    if coverage<POLICY['min_coverage']:reasons.append('low_skin_coverage')
    if sum(z>=POLICY['min_zone_coverage'] for z in zones)<POLICY['min_supported_zones']:reasons.append('localized_or_missing_zones')
    if count<POLICY['min_pixels']:reasons.append('too_few_pixels')
    if semantic_count/max(1,total)<POLICY['min_coverage']:stage='semantic_class_or_geometry_mismatch'
    elif coverage<POLICY['min_coverage']:stage='confidence_filter_loss'
    else:stage='spatial_support_check'
    return accepted,dict(geometry_pixels=total,semantic_pixels=semantic_count,accepted_pixels=count,
        coverage=coverage,confidence_retention=count/max(1,semantic_count),zone_coverages=zones,
        diagnostic_stage=stage,reasons=reasons,eligible=not reasons)

def iou(a,b):
    union=np.logical_or(a,b).sum()
    return float(np.logical_and(a,b).sum()/union) if union else None

def reject_overlapping_faces(ovals, qualities, threshold=.5):
    """Ambiguous overlapping detections: reject both rather than double-count skin."""
    pairs=[]
    for i in range(len(ovals)):
        for j in range(i+1,len(ovals)):
            overlap=iou(ovals[i],ovals[j]) or 0
            if overlap>threshold:
                pairs.append(dict(faces=[i+1,j+1],geometry_iou=overlap))
                for k in (i,j):
                    qualities[k]['eligible']=False
                    if 'overlapping_face_predictions' not in qualities[k]['reasons']:
                        qualities[k]['reasons'].append('overlapping_face_predictions')
    return pairs

def route_mask(mask,quality,stable=True):
    """Fail closed: rejected face contributes zero to regional adjustments."""
    if not quality['eligible'] or not stable:
        return np.zeros_like(mask,dtype=bool),'skip_regional_request_manual_review'
    return np.asarray(mask,bool),'eligible_for_diagnostic_regional_use'

def apply_regional(base,adjusted,mask,quality,stable=True):
    if base.shape!=adjusted.shape or mask.shape!=base.shape[:2]:raise ValueError('Geometry mismatch')
    safe,decision=route_mask(mask,quality,stable)
    return np.where(safe[...,None],adjusted,base),decision

def face_geometry(landmarks,shape,connections):
    h,w=shape;points=np.array([[v.x*w,v.y*h] for v in landmarks],np.int32)
    def hull(edges):return cv2.convexHull(points[sorted({v for c in edges for v in (c.start,c.end)})])
    oval=np.zeros(shape,np.uint8);cv2.fillConvexPoly(oval,hull(connections.FACE_LANDMARKS_FACE_OVAL),1)
    mask=oval.copy()
    for name in ('LEFT_EYE','RIGHT_EYE','LIPS','LEFT_EYEBROW','RIGHT_EYEBROW'):
        cv2.fillConvexPoly(mask,hull(getattr(connections,'FACE_LANDMARKS_'+name)),0)
    mask=cv2.erode(mask,np.ones((3,3),np.uint8)).astype(bool)
    return mask,oval.astype(bool)
