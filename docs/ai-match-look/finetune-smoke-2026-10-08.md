# CanonCGT head fine-tune smoke — 2026-10-08

## Scope and decision

Actual supervised CPU fine-tuning completed, diagnostic only. Do not promote either checkpoint to WPF. This is same-content reference reconstruction, not arbitrary-reference Match Look validation. User-approved customer JPEGs already have base presets; unknown base grades are not treated as neutral RAW. No existing review targets or human approval labels were used as optimization targets.

## Reproduce

Run from FotoAutomat root with the existing pinned `.local/canoncgt-env` and CanonCGT checkout/weights:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Train-CanonCgtSmoke.py --output .local/matchlook-data/finetune-smoke-NEW
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-CanonCgtSmoke.py
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Evaluate-CanonCgtSmoke.py
```

Evaluator currently targets `finetune-smoke-001`; change its path for another run. Artifacts stay local. The output directory must be new.

## Data and method

- 12 distinct source assets, four from each confirmed session. Session 01 trains, 02 validates, 03 tests; 8 known incremental recipes per asset, 32 examples per split. These are 12 independent source images, not 96 independent photographs.
- Target is recipe(source); reference is horizontally flipped target. This deliberately isolates a known color mapping from background/content confounding. It cannot establish transfer to unrelated references. All eight looks appear in every split; subject overlap remains unreviewed.
- Official SSL checkpoint SHA256 `5630c6dd5ceefa0d36d69ad298a5b87f1260dd02620a60af84cb4e259b4ad362` loaded strictly. Freeze encoder, canonicalizer and all Restyler except `Restyler.generate_lut.outc` (4,355 trainable parameters). Model stays eval mode to freeze normalization buffers.
- Cache frozen activations and canonicalized images at max 256 pixels. Adam lr 0.0001, gradient norm cap 1, seed 17, three epochs / 96 steps per variant. Full checkpoints saved separately with optimizer state and scope.
- Loss: Charbonnier whole-image reconstruction + skin weight × masked Charbonnier + 0.1 × out-of-range penalty + 0.01 × residual-LUT first-difference smoothness. Skin weight is 0 vs 0.5 in the ablation. Masks come from region-stability-002 safe masks; rejected regions contribute zero regional loss.
- This is a global LUT trained with regional supervision, not a spatial face correction model. No new face detector is trained. No explicit identity-consistency or monochrome-chroma loss yet; neutral/BW recipes alone did not solve these errors.

## Results

MAE in RGB units 0–255; lower is better, not a similarity percentage. Fixed three epochs, no selection using test.

| Metric | Original SSL | Global loss | Skin-weighted loss |
|---|---:|---:|---:|
| Train whole-image MAE | 18.357 | 14.118 | 13.988 |
| Validation whole-image MAE | 20.369 | 20.280 | 20.666 |
| Test whole-image MAE | 27.643 | 22.375 | 21.915 |
| Test accepted-skin MAE | 29.786 | 23.066 | 22.037 |

Skin MAE is measured from quantized output PNGs, averaged per example over available accepted masks. Whole-image metrics use clamped floating-point predictions. Same subjects/session create correlated samples; no significance claim. Skin-weighted model reduces test whole-image error ~20.7% and skin error ~26.0% relative to original, but validation error worsens ~1.5%. Therefore neither robust improvement nor >90% look agreement is demonstrated.

Visual inspection of six neutral/warm/BW cases: neutral and warm still too muted/gray; monochrome still retains color. Not production quality.

Training + evaluation with cached features took 1.18s and 1.12s respectively; these timings EXCLUDE feature preparation/model loading and are NOT end-to-end training or deployment latency.

## Verification and outputs

- Two loss tests pass: exact identity with empty mask remains finite/zero; regional weighting contributes expected magnitude.
- Runtime assertions: finite gradients/loss, disjoint source assets across training and other splits, all non-head tensors unchanged, saved checkpoint state reload exact.
- `.local/matchlook-data/finetune-smoke-001/`: manifest.json, report.json, skin-evaluation.json, baseline/global/skin-weighted test PNGs, two full checkpoints, index.html and overview.jpg.

## Next experiment

1. Review subject overlap and expand to independent sessions and scenery. Keep this diagnostic split separate from a final untouched evaluation set.
2. Add neutral identity and monochrome chroma constraints, evaluate per-look/per-region errors and clipping. Use validation for all tuning; reserve a new untouched test once current examples have informed development.
3. Establish valid cross-content supervision: shared verified base rendering plus same known recipe on distinct source/reference images, or administrator-corrected targets. Existing preset JPEG pairs are not assumed to have the same base rendering.
4. If head-only updates cannot represent look differences, unfreeze final Restyler block in a controlled ablation, with lower learning rate and frozen baseline comparisons. GPU training may accelerate this, but architecture/inference remain separately benchmarked for CPU.
5. Only after held-out quality passes: compose/export LUT and measure round-trip error plus real i7/16GB latency/RAM. A single global .cube cannot encode spatial face masks or per-image exposure/WB normalization; those remain separate pipeline operations.
