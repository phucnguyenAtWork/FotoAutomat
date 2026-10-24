# Controlled booth capture direction — 2026-10-12

## User-confirmed conditions

The booth room is darkened to control ambient illumination, with lighting/flash intended to illuminate faces evenly. Camera settings may be chosen to suit the Match Look LUT. This does not yet confirm manual WB/exposure lock, fixed flash power versus TTL, fixed modifiers/positions, or ambient contribution across sessions.

## Engineering direction (proposal, not deployed)

Prioritize a calibrated source pipeline for each camera + lens + lighting + camera-rendering configuration. Record WB, exposure, flash mode/power, camera JPEG picture style/film simulation, color space, firmware and calibration version. A capture profile is configuration-specific, not permanent per booth: changes require verification and potentially recalibration.

Proposed flow: captured image -> versioned booth input transform -> reusable creative look -> output conversion. Keep reference appearance intact; decode its orientation/ICC correctly without automatic aesthetic WB neutralization. Prefer a stable capture baseline and creative LUT strength over changing physical capture settings for every LUT. Intentional alternate capture modes need their own calibration records.

A ColorChecker-derived DNG profile cannot simply be applied as a JPEG RGB matrix. RAW and rendered JPEG need distinct processing contracts; no RAW decoder/profile integration is claimed here. CPU costs must be measured on the actual target machine.

## Corrections to earlier interpretation

The uint8 average drift 1.15/255 alone does not establish model instability or visual imperceptibility; inspect per-case/ROI distributions and images. Accepted JPEG case means have biased denominators. Paired correction/bypass metrics must use all the same cases, with rejected branches explicitly represented.

In controlled synthetic tests, codec differences can be attributed by comparing before/after codec stages. For arbitrary JPEGs, small edge excursions versus connected highlight/skin regions are diagnostic features, NOT proof of codec artifacts versus sensor clipping. Genuine clipped highlights may be small; white objects may form large regions. Use known provenance, correction excursion size, spatial support and regional risk; expose uncertainty and a bypass fallback.

## Next ablation plan — not training authorization

Compare unchanged baseline, bounded preprocessing, EV/WB/JPEG augmentation fine-tune without preprocessing, and the combination. Keep reference/target style semantics consistent, group all augmentations by original image before splitting, use identical evaluation cases and report skin/highlight/background, range and neutral/BW criteria. Existing creative JPEGs do not supply unknown original WB or exposure ground truth. Notify user before any fine-tuning.

Open operational question: are manual exposure, fixed/custom WB and manual flash power acceptable, or must AWB/autoexposure/TTL remain active? No camera control or hardware test is authorized by this document.

Reference reading: Calibrite camera DNG profiling https://calibrite.com/us/learning-centre/calibrite-profiler-camera-dng/ and white-balance targets https://calibrite.com/product/colorchecker-white-balance/ . Senior Computer Vision skill consulted as methodology guidance, not camera color science evidence or installed executable tooling.

## Decision confirmed by user — subsequent discussion 2026-10-12

User chose locked camera settings, manual flash combined with continuous lighting, and one baseline capture configuration shared by creative LUTs. Intended baseline: manual exposure, fixed/custom WB, manual flash power, fixed camera JPEG rendering. This is a product/design decision, not verified SDK support or authorization to operate hardware.

Continuous light remains on during exposure: final capture receives continuous light plus flash, while live view normally sees continuous light only. Maintain separate versioned preview and capture transforms feeding the same creative LUT. Calibration and comparison must include chart/skin captures under the actual combined lighting; do not promise matching preview/final colors before testing. Confirm exact hardware/settings and compare one person and four people at marked positions. Any changed lighting/camera-rendering configuration requires verification.
