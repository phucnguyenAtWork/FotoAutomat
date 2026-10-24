# Region audit — 08/10/2026

Implemented `Audit-PortraitRegions.py`, isolated `.local/region-audit-env` (MediaPipe 1.1.0, Pillow 12.3.0, NumPy 2.5.3, OpenCV contrib 5.0.0.93; full freeze stored locally). CPU explicitly selected, XNNPACK confirmed. No customer uploads or original modification. No WPF integration or training.

## Algorithms actually run

MediaPipe Face Landmarker v1 (20-face maximum, detection/presence thresholds 0.3) and Selfie Multiclass 256×256 float32 v1. Six classes: background, hair, body skin, face skin, clothes, accessories. The model card describes a Vision Transformer segmentation architecture and Apache 2.0 model license; masks are not pixel-perfect. This is not a fine-grained lip/eye parsing network.

Per-face measurement ROI: landmark face-oval hull, subtract eye/lip/eyebrow hulls, erode 3×3, intersect face-skin class and confidence ≥0.7. Export individual binary masks; do not infer identities. Hair/occlusion errors remain possible. Confidence scores are not calibrated accuracy.

Model downloads pinned to `/1/`, SHA256 recorded in report:
- https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task
- https://storage.googleapis.com/mediapipe-models/image_segmenter/selfie_multiclass_256x256/float32/1/selfie_multiclass_256x256.tflite
- Segmentation model card: https://storage.googleapis.com/mediapipe-assets/Model%20Card%20Multiclass%20Segmentation.pdf
- Task documentation: https://developers.google.com/edge/mediapipe/solutions/vision/image_segmenter and https://developers.google.com/edge/mediapipe/solutions/vision/face_landmarker

Do not treat documentation code license as automatic proof of all bundle component licenses for redistribution; deployment license provenance remains a separate check.

## Dataset review and results

Selected **12 distinct sources**, one per original probe, with looks neutral/warm/low-key/cool/pastel/high-contrast/monochrome/film-matte/cool/neutral/pastel/film-matte. Unlike previous ablation with repeated source-01, this covers all 12 source probes and three sessions. 22 unique source/reference pixel inputs processed (cache by hash). All source/reference/target hashes checked.

31 detected faces across 12 source images; counts 7,1,1,6,2,2,2,2,2,2,2,2. All 31 have at least 50 skin ROI pixels. Source contact sheet inspected: coarse person/hair/background separation is plausible; bangs, eyewear and occlusion still require mask review. Reference masks have not all been visually validated. No annotated ground truth: **do not report recall, IoU or detection accuracy** from these counts. Expected face count remains null in report, mask review pending. Nonportrait/no-face fallback remains untested.

Median combined face+segmentation time across 22 inputs: **0.306 seconds**, after one warmup and three measured calls per image; excludes model load/file I/O, on current workstation, not certified NUC performance. Export gallery, label PNGs, per-face mask PNGs and review JSON interface in `.local/matchlook-data/region-audit-001`. `highlights.html` adds target/SSL/E2E overlays; magenta marks new near-ceiling skin pixels compared with target.

Fixed source masks used for target/model comparison, avoiding re-detection after grading. Metrics: encoded RGB MAE, **DeltaE76 (not DeltaE00)**, median Lab L difference, any-channel ≥250 ratio and new near-ceiling pixels versus target. Examples: case-01-neutral SSL 51.35%, E2E 0.89%; case-04-cool SSL 72.61%, E2E 8.71%. These are percentages of predicted ROI pixels crossing a threshold, not similarity/accuracy or proof of lost texture. Need audited ROIs and local-detail analysis before stronger conclusions.

Reference–target semantic validity remains **pending for every case**: original JPEGs already have different unknown presets; identical incremental transforms do not ensure an absolute shared look. No cases promoted to ground truth or train-ready. Need manual target/reference look alignment or a canonical source set; segmentation alone does not solve this.

## Verification and use

`Test-PortraitRegions.py`: 3 tests PASS (empty ROI, identity metrics, new clipping/ROI isolation). Actual CPU inference completed; no UI interaction test. Reviewer can mark mask issues in index.html and export region-review.json; no importer or automatic training approval.

```powershell
.local/region-audit-env/Scripts/python.exe tools/MatchLookData/Test-PortraitRegions.py
.local/region-audit-env/Scripts/python.exe tools/MatchLookData/Audit-PortraitRegions.py --output .local/matchlook-data/region-audit-002
```

Next work: correct rejected masks, validate no-face scenes and reference masks, then test region-conditioned normalization/matching with masks reviewed. Do not tune on evaluation targets or claim the current overlay stage improves grading.
