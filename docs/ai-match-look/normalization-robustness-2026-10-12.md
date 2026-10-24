# Normalization robustness — 2026-10-12

## Executed

`tools/MatchLookData/Audit-NormalizationRobustness.py`: 96 cases, 12 development JPEG sources, a fixed reference from another session per source, unchanged official SSL weights. CPU four threads, 256px long edge, 30.204 seconds. Model state hash unchanged and gradients absent. Eight normalization regression tests passed. No training, no uploads, no WPF changes.

Controlled perturbation: -0.6 EV and linear RGB gains (1.1,1,0.9), then correction with known inverse or explicit parameter errors. Codec branches quantize to uint8, or encode/decode JPEG quality 90/70 with 4:2:0 subsampling. Original customer JPEGs are already graded. These are simulated changes, not physical camera relighting or RAW WB.

## Results

| Variant | Correction blocked /12 | Corrected output MAE /255 vs original SSL baseline |
| --- | ---: | ---: |
| Exact inverse, float | 0 | 0.0000454 |
| Exact inverse, uint8 | 0 | 1.1486 |
| Exact inverse, JPEG90 | 10 | 4.5826 (only 2 accepted cases) |
| Exact inverse, JPEG70 | 11 | 4.6503 (only 1 accepted case) |
| JPEG90, EV error -0.1 | 7 | 3.2878 (only 5 accepted cases) |
| JPEG90, EV error +0.1 | 12 | unavailable |
| JPEG90, red correction gain +3% | 11 | 3.2033 (only 1 accepted case) |
| JPEG90, EV error +0.3 | 12 | unavailable |

Accepted-case means have different denominators and must NOT be ranked as evidence one correction is better. Blocked cases have no corrected model inference. Uncorrected means in report.json cover all 12, so direct aggregate before/after comparisons would also be biased when any cases are blocked. This measures consistency with baseline, NOT target look similarity or a percentage of Capture One quality.

## What the experiment found

The current adapter rejects any correction with out-of-range linear RGB beyond numerical tolerance. JPEG quantization/subsampling can move edge/channel values enough that even the exact known inverse gets rejected. Maximum outside-channel fraction was 1.593% for JPEG90 and 2.095% for JPEG70. This is not floating-point error; increasing the 1e-6 tolerance would conceal a different issue. Positive EV error +0.1 reached 9.334%, +0.3 reached 25.018% on this sample.

Even uint8 quantization raised output drift to 1.15/255 despite exact correction. Skin per-case drift ranged 0.278–3.948/255. The model can amplify small input changes. Existing baseline faces are still visibly too bright in the inspected group portrait; successful recovery of baseline is not visual improvement.

## Decision and next implementation

Keep normalization optional, known/manual only. Do not enable automatic correction or start fine-tuning from this result. The adapter's binary range guard needs a separate, explicit policy that distinguishes tiny codec excursions from widespread clipping and reports affected skin/highlight regions. Any permitted range mapping must be measured for color/detail loss; do not silently loosen numerical tolerance or treat clipping as recoverable.

Next: implement and compare a bounded correction policy against bypass on these same cases, with paired denominators, raw range diagnostics and skin/highlight errors. Then validate any exposure/WB estimator independently. Automatic estimation, disentangled style LUT, photographic cross-content targets and high-resolution checks remain unfinished.

## Artifacts

`.local/matchlook-data/normalization-robustness-001/report.json` contains all 96 cases. `index.html` illustrates eight variants for the first source only. `overview.jpg` was visually inspected; shows float/uint8/JPEG90 comparisons. HTML itself was not browser-tested. Customer artifacts remain local and uncommitted.

Four-priority status: (1) checkpoint quality gate wired, thresholds provisional; (2) real cross-content training targets pending; (3) explicit normalization implemented and robustness weakness identified, no automatic estimator; (4) multi-face ROI development checks at 256px, full-resolution/landscape validation pending.
