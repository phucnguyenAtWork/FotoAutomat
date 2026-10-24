# Pre-training preparation — 2026-10-09

User instruction: prepare and optimize first; notify user before any further fine-tuning. No training or optimizer steps executed in this preparation pass. Synthetic autograd tests only; no CanonCGT weights loaded or changed.

## Implemented

`tools/MatchLookData/MatchLookLosses.py` provides separately observable loss terms:

- Charbonnier global reconstruction against a supervised target.
- Charbonnier accepted-skin reconstruction against the SAME target: preserves desired skin grading instead of forcing all skin toward source color. Per-sample normalization; absent masks do not dilute valid samples.
- Neutral identity constraint only for explicit verified neutral examples; inconsistent labels raise an error.
- Monochrome channel variance penalty only for explicit achromatic targets; colored targets cannot be mislabeled monochrome.
- Target-aware near-ceiling overshoot penalty: intended white targets are not penalized. This is an encoded-RGB proxy, not proof of preserved physical highlight detail.
- Gamut and smoothness of residual LUT.

Weights in `finetune-config.draft.json` are provisional, NOT optimized by evidence. Config outlines maximum 30 epochs, patience 5, validation-only checkpoint selection and head-only initialization. A complete trainer with resume/early stopping and these losses is still pending. Old smoke trainer intentionally remains unchanged to preserve reproducibility. There is no runtime training guard on that old script; this preparation tool itself cannot launch training.

## Verification

Seven synthetic tests pass: identity/empty-mask gradients, monochrome gradient direction, intended-white handling, invalid monochrome rejection, mask locality, batch empty-mask normalization, nonfinite rejection. This validates formulas, not model quality or optimal loss weights.

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-MatchLookLosses.py
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Audit-FinetuneReadiness.py --output .local/matchlook-data/finetune-readiness-2026-10-09.json
```

Audit output must be a new file. Dataset files read only. Current audit: 176/176 standardized files match hashes; 176 unique exact hashes; 3 confirmed sessions (90/56/30); 96 diagnostic triplets structurally valid. Exact uniqueness does not imply no near-duplicates or no shared subjects. All 176 are already-graded JPEGs.

## Not ready to train

- Existing manifest still diagnostic, without production splits or locked holdout. Previous smoke test is development evidence now, not an untouched final test.
- Subject overlap/near-duplicate review unresolved. Need anonymous grouping from metadata/manual review; face localization is not identity recognition.
- Cross-content paired supervision lacks verified common base rendering or administrator-corrected targets. Applying one incremental recipe to two differently graded JPEGs does not resolve this.
- Landscape coverage and dataset rights/quality need verification before inclusion; do not treat Open Images metadata as blanket permission or quality approval.
- Target approval is still pending in the machine-readable manifest. Prior palette approval is not silently promoted to every target approval.
- Spatial mask gates already exist, but threshold calibration and rejected-region review are incomplete.

## Next deliverables before notification to train

1. Build a review manifest for base-look groups, anonymous subject/session groups, rights and target provenance. Separate diagnostic data from final evaluation.
2. Prepare cross-content triplets only within verified base-rendering groups or from explicitly corrected targets. Add diverse landscape cases, and neutral/BW cases without forcing every creative look to natural skin.
3. Implement a trainer that consumes validated manifests, records separate loss terms, enforces split isolation, resumes exactly and selects checkpoints by validation; verify with synthetic tensors without optimizing CanonCGT.
4. Lock data/config hashes and evaluation thresholds. Report readiness and expected compute to user BEFORE starting the run.

No claim that all optimization is complete. No new quality gain is claimed from code-only changes. A global LUT cannot express spatially different treatment of identical RGB colors; regional supervision does not change that representation limit.

## Follow-up: local training review manifest

Generated `.local/matchlook-data/training-review-001/index.html` with 176 thumbnail cards and a hash-bound JSON template. `Build-TrainingReview.py` verifies original standardized hashes and only writes thumbnails/output in a new directory. Inputs remain unchanged. Fields: base preset, anonymous person-group codes, near-duplicate group, domain, review/exclude decision, notes. No automatic identity inference. Empty/unknown base looks stay unresolved. Browser export/import supports resuming review; changes are NOT automatically saved, so export before closing.

`TrainingReview.py` checks dataset hash, exact asset set, decisions and required labels. Four unit tests pass (valid labels cannot authorize training; unknown base remains pending; changed hash and duplicate rows rejected). This module does not yet assign splits or validate target provenance. It is NOT wired to a production training runner. HTML was generated and requested open, but browser interactions have not been automatically exercised.

Commands:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Build-TrainingReview.py --output .local/matchlook-data/training-review-NEW
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-TrainingReview.py
```

The next implementation is split leakage validation and a resumable trainer harness tested with synthetic inputs only. Real fine-tuning remains paused pending readiness and user notification.

## Requested allocation 85/15

User requested 85% train / 15% test. Created `.local/matchlook-data/split-85-15-001/manifest.json` with seeded image-level allocation: 150 train (85.23%), 26 test (14.77%), seed 20261009, verified all source hashes, unique/disjoint IDs. Original dataset unchanged. Reproduce with `tools/MatchLookData/Create-RequestedSplit.py` from repository root; requires new output directory.

This explicitly prioritizes the requested ratio over session isolation. Whole-session split nearest the target would be 146/30 (82.95/17.05). All three sessions occur in both sets; this allocation is for development, NOT evidence of new-session generalization. Near-duplicate and subject checks remain unresolved. Existing diagnostic exposure means test is not a fresh final holdout. Training remains disabled in manifest. Any validation subset must come from the 150 training assets, never the 26 test assets. Graded JPEG roles remain reference/self-supervised input, not paired ground truth. No fine-tuning executed.

## 2026-10-09: split audit and curriculum proposal

Ran `Prepare-TrainingAudit.py`, output `.local/matchlook-data/pretrain-audit-001`. Verified all source hashes; examined all 150×26 cross-split pairs with 64-bit horizontal dHash, candidate threshold <=10 (not calibrated). Found 108 candidate pairs involving 51 train assets; proposed quarantine only, no deletion or mutation of original split. Remaining 99 assets: sessions 01/02/03 = 31/47/21. Proposed validation holds session03 within this remaining pool: 78 development train sources, 21 validation sources, original 26 test preserved. Session overlap with test persists.

Generated 792 recipe manifest entries (99×8), NOT rendered image files or independent new photographs. Each is a known incremental transform on the same source, with transformed same-content reference. This is explicitly a diagnostic/self-supervised curriculum, not valid cross-content supervision. No claim of optimized arbitrary-reference learning. Assertions verify no test parents enter curriculum and train/validation source sets are disjoint.

Visually inspected 12 lowest-distance candidate pairs in overview.jpg. Some are very similar consecutive poses, but there are obvious false positives between different subjects against the same backdrop. Therefore 51 quarantined assets are provisional, not 51 proven duplicates. Do not finalize exclusions or claim leakage resolved based only on dHash. Further verification must use structure/content evidence; no automatic identity recognition was performed.

Next: refine candidate verification (avoid background-only similarity), then final allocation and synthetic-only trainer tests. No CanonCGT training, model loading, or optimizer updates executed in this audit.

## Follow-up: structural ranking and synthetic trainer harness

`Rank-DuplicateCandidates.py` ranked all 108 candidates using cosine similarity of grayscale finite-difference gradients in a fixed central crop. Generated `.local/matchlook-data/duplicate-structure-001/report.json` and three contact sheets. This is neither semantic segmentation nor identity recognition. Inspected the first 36 ranked pairs: strongest pairs have closely matching poses; lower scores include changed clothing/poses and different subjects on similar backgrounds. No automatic release threshold is validated, so quarantine remains provisional and original split is unchanged. Gradient alignment is sensitive to movement; low score cannot prove no leakage.

`TrainingHarness.py` adds generic epoch-boundary state, validation-only early stopping, model/Adam/RNG checkpoint restore and dataset/config fingerprint rejection. `Test-TrainingHarness.py` uses a tiny synthetic Linear model only, never CanonCGT/customer images. Tests cover stopping, invalid metrics, exact uninterrupted-vs-resumed next-step weights, and fingerprint mismatch. Scope is deterministic CPU fixtures; CUDA, multiworker loader, NumPy RNG, mid-epoch resume and production integration remain pending. This is a harness component, not a completed CanonCGT trainer. No CanonCGT fine-tuning performed.

## Local continuation after cancelling LangSmith

User cancelled paid API judge setup; no cloud uploads or judge calls were performed. Added `SupervisedLoop.py`: cached-feature forward callback, separately logged loss terms, finite-gradient checks and clipping, validation macro-average over looks, parent-ID overlap rejection and prohibition on test examples for updates/checkpoint selection. Three synthetic integration tests pass: model bias updates with frozen weights unchanged; validation does not update weights; shared parents and test-partition misuse rejected. Tests use a tiny Linear fixture only, not CanonCGT or customer photographs.

This loop is reusable preparation, not a complete production trainer. CanonCGT adapter, end-to-end epoch/checkpoint orchestration, final data manifests and real-data checks remain pending. `TrainingHarness.py` resume tests were completed separately; the new loop has not yet been covered by an integrated resume equivalence test. No further fine-tuning started. LangSmith draft files retained, inactive.

## CanonCGT adapter and epoch pipeline integration

Added `CanonAdapter.py`: pinned SSL checkpoint hash and strict state loading; disables pretrained MobileNet download during initialization then restores factory; freezes all except Restyler.generate_lut.outc, keeps eval mode. `prepare` runs frozen feature extraction under no_grad and caches canonicalized source and Restyler head input. `forward` rebuilds LUT and applies trilinear sampling; gradients can flow to head when explicitly training.

Added `TrainingPipeline.py`: explicit authorization/readiness guards, parent split validation, Adam, named loss logging via SupervisedLoop, validation macro MAE, early stopping, best/last checkpoints, fingerprint-based resume and JSONL history. It receives prepared examples and a manifest/config from caller; no automatic production loader or CLI starts it. Flat config expected by this API differs from nested draft config; translating/locking production config remains pending. Caller must provide real provenance in manifest; boolean flags alone are not a completed readiness audit. Existing cached features valid only while backbone stays frozen.

`Test-TrainingPipeline.py` checks real SSL forward parity with synthetic 64px tensors and full unchanged state, head-only trainability, training guard, plus synthetic tiny-model epoch/save/completed-resume integration. No optimizer update on CanonCGT. Earlier generic harness test covers exact next-step resume; this new integration test covers completed-run resume, not interruption mid-epoch. No customer photo used. CPU-only; no WPF integration or model quality claim.

Flow: prepared source/reference -> SSL encoder/canonicalizer/frozen Restyler -> cached canonical image + head features -> trainable output head -> LUT -> trilinear image -> supervised losses -> (authorized training only) Adam -> validation -> early stop + best/last checkpoint. Test partition is not accepted by optimization or validation selection. Final held-out evaluation is separate and not implemented by this orchestrator.
