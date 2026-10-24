"""Stream Open Images metadata; select review candidates, never grant training rights."""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path

SCENES = ('Room', 'Landscape', 'Mountain', 'Forest', 'Beach', 'Lake')
FILES = ('oidv7-class-descriptions.csv', 'oidv6-train-annotations-bbox.csv',
         'oidv7-train-annotations-human-imagelabels.csv', 'train-images-boxable-with-rotation.csv')

def rows(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        yield from csv.DictReader(stream)

def face_area(row):
    if any(row[k] != '0' for k in ('IsOccluded', 'IsTruncated', 'IsGroupOf', 'IsDepiction')):
        return None
    x0, x1, y0, y1 = (float(row[k]) for k in ('XMin', 'XMax', 'YMin', 'YMax'))
    if not (0 <= x0 < x1 <= 1 and 0 <= y0 < y1 <= 1):
        return None
    area = (x1-x0)*(y1-y0)
    return area if area >= .04 and x1-x0 >= .12 and y1-y0 >= .12 else None

def select(metadata, output, portraits=700, per_scene=50):
    metadata, output = metadata.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(metadata):
        raise ValueError('Use a new output directory outside input metadata')
    inputs = [metadata/name for name in FILES]
    before = [(p.stat().st_size, p.stat().st_mtime_ns) for p in inputs]
    classes = {r['DisplayName']: r['LabelName'] for r in rows(inputs[0])}
    face_id = classes['Human face']
    scene_ids = {classes[name]: name for name in SCENES}
    faces = {}
    print('Scanning face boxes...', flush=True)
    for row in rows(inputs[1]):
        if row['LabelName'] == face_id:
            area = face_area(row)
            if area is not None:
                faces[row['ImageID']] = max(area, faces.get(row['ImageID'], 0))
    print(f'{len(faces)} images with qualifying face boxes. Scanning verified labels...', flush=True)
    candidates = {image_id: {'portrait'} for image_id in faces}
    for row in rows(inputs[2]):
        if row['LabelName'] == face_id and row['Confidence'] == '0':
            candidates.get(row['ImageID'], set()).discard('portrait')
        if row['Confidence'] != '1':
            continue
        label, image_id = row['LabelName'], row['ImageID']
        group = 'portrait' if label == face_id and image_id in faces else scene_ids.get(label)
        if group:
            candidates.setdefault(image_id, set()).add(group)
    print(f'{len(candidates)} label candidates. Joining provenance...', flush=True)
    pools = {name: [] for name in ('portrait', *SCENES)}
    for row in rows(inputs[3]):
        image_id = row['ImageID']
        if image_id not in candidates or row['Subset'] != 'train':
            continue
        if row['License'].replace('http://', 'https://').rstrip('/') != 'https://creativecommons.org/licenses/by/2.0':
            continue
        if not all(row[k] for k in ('OriginalLandingURL', 'OriginalURL', 'Author', 'AuthorProfileURL')):
            continue
        for group in candidates[image_id]:
            pools[group].append(row)
    chosen, used, authors = [], set(), Counter()
    for group, pool in pools.items():
        quota = portraits if group == 'portrait' else per_scene
        count = 0
        for row in sorted(pool, key=lambda r: hashlib.sha256(('fotoautomat-oi-v1:'+r['ImageID']).encode()).digest()):
            if count >= quota:
                break
            if row['ImageID'] in used or authors[row['AuthorProfileURL']] >= 5:
                continue
            used.add(row['ImageID']); authors[row['AuthorProfileURL']] += 1; count += 1
            chosen.append(dict(row, review_group=group, face_area_fraction=faces.get(row['ImageID']),
                reason='Unoccluded annotated face box >=4% image area; no explicit negative face label' if group == 'portrait' else 'Verified scene label; people may also be present',
                rights_review='pending', visual_review='pending', training_ready=False))
    if before != [(p.stat().st_size, p.stat().st_mtime_ns) for p in inputs]:
        raise RuntimeError('Input metadata changed during selection')
    output.mkdir(parents=True)
    (output/'candidates.json').write_text(json.dumps(chosen, ensure_ascii=False, indent=2), encoding='utf-8')
    if chosen:
        with (output/'review.csv').open('w', encoding='utf-8-sig', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=list(chosen[0])); writer.writeheader(); writer.writerows(chosen)
    (output/'image-ids.txt').write_text(''.join('train/'+r['ImageID']+'\n' for r in chosen), encoding='utf-8')
    summary = dict(selected=len(chosen), groups=dict(Counter(r['review_group'] for r in chosen)),
        eligible_pools={k:len(v) for k,v in pools.items()}, unique_authors=len(authors), max_per_author=5,
        requested=portraits+len(SCENES)*per_scene, images_downloaded=0, training_ready=False,
        inputs=[dict(name=p.name, bytes=s[0], mtime_ns=s[1]) for p,s in zip(inputs,before)],
        limitations=['Metadata filtering only; no pixel quality or skin-tone assessment',
            'CC BY listing is not verified permission/model release',
            'Bounding boxes are not skin masks; no identity inference',
            'Scene categories overlap; no assumption that scene images have no people',
            'No before/after color targets; no train/test allocation yet'])
    (output/'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary, indent=2), flush=True)
    return chosen

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--metadata', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    select(args.metadata, args.output)
