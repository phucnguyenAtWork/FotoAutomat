# Region stability gates — 08/10/2026

Implemented four requested checks in research Python pipeline, not WPF:

1. `RegionQuality.assess`: keeps separate geometric ROI, semantic class intersection and confidence-threshold intersection counts. `Audit-RegionStability` records coverage at confidence 0.5/0.7/0.9 to diagnose filtering sensitivity. No automatic threshold relaxation. Missing geometric match on perturbed input is explicitly recorded, not inferred as missing skin.
2. Coverage gate replaces the previous >50-pixel heuristic: >=100 pixels, >=35% eligible geometry coverage and >=15% support in at least three of four bounding-box quadrants. These are provisional conservative gates, not calibrated accuracy or anatomical cheek labels. `reject_overlapping_faces` flags pairs with oval IoU >0.5 and rejects both as ambiguous; true overlapping faces may also be rejected.
3. Original, identical repeat, encoded-RGB ×0.7, ×1.3 clipped, grayscale and half-size inference, masks mapped back to original coordinates. Geometry-only greedy one-to-one matching, minimum oval IoU 0.3; skin consistency IoU >=0.65 plus quality gate for all perturbations. Not identity recognition, not ground-truth IoU and not physical EV changes. Six variants per input; image count refers to cached unique inputs, not people.
4. `route_mask` returns zero mask and `skip_regional_request_manual_review` for rejected/unstable faces. `apply_regional` preserves base pixels there. Runner exports safe masks using this gate. No WPF caller or automatic grading integration yet; flags do not create a manual mask editor. A missed face cannot safely be assumed to be background.

Output: `.local/matchlook-data/region-stability-002/index.html`, report.json and safe-mask PNGs. Version 001 retained before adding overlap rejection. Six unit tests PASS: small patch (>50 pixels) rejection, confidence loss, empty ROI, rejected/unstable fallback preserves pixels, spatially localized rejection, duplicate overlap rejection.

## Findings

Completed 22 unique inputs × 6 variants = 132 inference pairs. Of 60 original face predictions (including duplicates, not unique people), 42 pass the combined gates and 18 route to skip/manual review. Identical-repeat matched-mask IoU minimum is 1.0; perturbation robustness is substantially weaker. Case-12 reference has an overlapping original prediction pair with oval IoU 0.9000. No model accuracy percentage inferred.

Case-07 monochrome reference: failed face has 16,276 geometric pixels, 8,001 semantic-class pixels, only 540 after confidence 0.7: **3.32% coverage**. At confidence 0.5 coverage is still only **24.15%**. Landmark detection exists; semantic support and especially confidence filtering lose coverage. This does not establish grayscale as the sole cause. New gate rejects it instead of accepting >50 pixels.

Case-12 film-matte reference: original face predictions overlap; visual inspection shows missing usable mask on the far-left profile, while half-size inference produces a partial mask there. Total face count alone is insufficient. New overlap and perturbation gates reject ambiguous predictions; no claim that segmentation itself is fixed.

Limitations: gating avoids acting on some unreliable masks but does not improve segmentation weights. Small/profiling/occluded faces and confidence calibration need further validation. Repeatability on identical input is different from robustness to image changes. Original reports and user review remain immutable; this is a new diagnostic run.

Run from repo root:

```powershell
.local/region-audit-env/Scripts/python.exe tools/MatchLookData/Test-RegionQuality.py
.local/region-audit-env/Scripts/python.exe tools/MatchLookData/Audit-RegionStability.py --output .local/matchlook-data/region-stability-003
```
