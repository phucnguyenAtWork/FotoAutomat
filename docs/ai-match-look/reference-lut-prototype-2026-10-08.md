# Reference LUT prototype — 08/10/2026

Implemented research algorithms in `tools/MatchLookData/ReferenceLutPrototype.py`; not connected to WPF or published presets. Existing Core CubeLut still uses trilinear interpolation; prototype uses tetrahedral, so desktop numerical parity is not claimed.

## Implemented

- Normalized 3D `.cube` read/write, red-fastest order, size 2–65, finite values and row-count checks. Reject 1D, combined/shaper and non-unit domains rather than guessing. Caller must supply encoded sRGB input; no arbitrary log-camera LUT compatibility claim.
- Chunked tetrahedral interpolation and Amount blending.
- Global RGB sliced **2-Wasserstein squared** fitting: 2,048 sampled pixels/image, 32 fixed seeded projections, 100 Adam steps, 17³ LUT. Target is never given to fitter.
- Ablation with penalties: first differences of residual LUT, identity distance, output range, and brightening on source channels above 0.9. This last term is a highlight heuristic, not a skin mask.
- Conservative alternative: 17-knot independent RGB curves, positive softplus increments normalized to endpoints 0/1, smoothness and identity penalties. This prevents channel reversal; not semantic matching, full color grading, or exposure/WB separation.

Global SWD is a baseline inspired by the general projection/sorting distance, not an implementation of MS-SWD or Capture One. References: https://arxiv.org/abs/1706.02631 and https://colour.readthedocs.io/en/develop/generated/colour.algebra.table_interpolation_tetrahedral.html .

## Executed comparison

`Run-ReferenceLutPrototype.py` ran 12 explicitly selected diagnostic cases: all eight looks on case-01 plus case-03-low_key, case-09-cool, case-11-pastel, case-12-film_matte. Includes previously accepted examples. Dataset hashes and review-report hash checked. Source/reference only used for fitting; target loaded afterward for metrics. No training, target tuning or human approval inferred.

Current output `.local/matchlook-data/reference-lut-prototype-002` contains report, 36 LUTs and comparison HTML/overview. Initial failed ablation retained in `reference-lut-prototype-001`. These are diagnostic exports, not approved production filters.

| Method | Mean RGB MAE / 255 on these 12 cases |
| --- | ---: |
| Frozen SSL | 21.54 |
| Frozen E2E | 18.76 |
| Global SWD free LUT | 66.91 |
| Global SWD regularized | 65.89 |
| Monotone channel curves | 61.49 |

**Quality gate failed.** Free LUT produces tone reversals/patchy faces; regularization insufficient. Monotone curves avoid channel reversal but global reference statistics still drive inappropriate brightness/color shifts. Do not replace CanonCGT with these methods. MAE is not similarity percentage; selected diagnostic cases are not held-out evaluation. Reference/target also retain different base presets, so their shared incremental recipe does not imply identical absolute look.

36 cube roundtrips had maximum float difference 0 in this renderer. Five tests passed: identity/Amount, affine interpolation, cube order/roundtrip, malformed input rejection, known-shift improvement and monotone bounds (some grouped in one test). Tests required unsandboxed execution because Windows sandbox temp files denied writes. No .NET changes or WPF verification.

## Next unresolved work

Face parsing/landmarks and region correspondences remain unimplemented in this prototype. Need independently verified masks and reference–target look consistency before regional optimization or fine-tuning. No claim that a global distribution objective can infer camera settings or protect faces. A global LUT cannot encode spatial masks. Full production plan remains open; this increment rejects a weak global-only route with measured evidence.

Commands from repository root:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-ReferenceLutPrototype.py
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Run-ReferenceLutPrototype.py
```

Runner uses the documented local dataset paths and a new-output guard; change its output version before rerunning. Customer assets remain local and uncommitted.
