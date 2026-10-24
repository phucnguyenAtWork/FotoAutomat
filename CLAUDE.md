# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Read [AGENTS.md](AGENTS.md) first.** It is the authoritative agent harness: the confirmed product scope (partly in Vietnamese), the hardware decisions, the phase roadmap, open decisions, and the development rules. This file adds only the commands and architecture overview, and does not repeat it.

## What this is

FotoAutomat is commercial Windows photobooth software (C#/.NET 10, WPF) for low-spec booth PCs that must capture, process, and print with no Internet access. **The current stage is a simulator foundation.** No real camera, printer, or money acceptor is integrated. Keep status claims accurate, and never call a simulated device, placeholder recipe, or UI mock a production integration (see [CONTEXT.md](CONTEXT.md) for domain vocabulary).

## Commands

SDK pinned by `global.json` (10.0.401, latestPatch). Use `FotoAutomat.slnx` on Windows (includes WPF Desktop); `FotoAutomat.Portable.slnx` excludes Desktop.

```sh
./scripts/verify.ps1                                   # full verification (restore, format check, build, test, harness)

dotnet restore FotoAutomat.slnx --locked-mode
dotnet format FotoAutomat.slnx --verify-no-changes --no-restore   # CI fails on format drift; drop the flag to apply
dotnet build FotoAutomat.slnx --configuration Release --no-restore
dotnet test tests/FotoAutomat.Tests --configuration Release --no-build --no-restore

# single test / class (xunit)
dotnet test tests/FotoAutomat.Tests --filter "FullyQualifiedName~BoothWorkflowTests"
dotnet test tests/FotoAutomat.Tests --filter "FullyQualifiedName~BoothWorkflowTests.SomeTestName"

# CLI harness scenarios: happy, camera-failure, print-unknown, restart-recovery (JSON per scenario, nonzero exit on failure)
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build -- --list
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build -- --scenario print-unknown

# synthetic Match Look baseline (not a camera-quality benchmark)
dotnet run --project tools/FotoAutomat.MatchBenchmark

# desktop (Windows only)
dotnet run --project src/FotoAutomat.Desktop                         # customer UI (or operator workspace if no PIN set)
dotnet run --project src/FotoAutomat.Desktop -- --admin              # operator workspace (LayoutWindow)
dotnet run --project src/FotoAutomat.Desktop -- --workflow-simulator # durable SQLite session simulator
```

Build settings (`Directory.Build.props`): nullable enabled, **TreatWarningsAsErrors**, latest analyzers, lock files required. Package versions are central in `Directory.Packages.props`. For an intentional update, edit that file, restore without `--locked-mode`, and commit the changed `packages.lock.json` files. Never disable locked mode to fix a restore failure.

There is no automated WPF UI test. A Windows build or cross-build checks compilation, not runtime behavior. See [docs/testing.md](docs/testing.md) for the manual desktop checklist.

## Architecture

Three projects with a strict dependency direction (`Desktop → Infrastructure → Core`; Harness and Tests reference Core + Infrastructure):

- **Core** (`net10.0`, no UI/DB/SDK references) holds two largely independent areas:
  1. **Session workflow**: `BoothWorkflow` drives `SessionStage` (AwaitingPayment → Paid → Capturing → Captured → Printing → Completed / NeedsReview) through the ports in `Ports.cs` (`ICamera`, `IPrinter`, `IBoothStore`). Each transition carries the expected stage and revision, so stale writes are rejected. Intent is persisted before the side effect. On restart, an interrupted Capturing or Printing session goes to NeedsReview and is never auto-retried. The session ID is the print job ID. `CustomerPreviewFlow` is a separate, in-memory `INotifyPropertyChanged` state machine for the 11+ customer UI screens (payment branches, countdowns, review timers, slot filling). It does not persist anything.
  2. **Image processing**: `ColorRecipe` plus the static `ColorProcessor` (with internal `AdvancedColorProcessor` and `ExtendedColorProcessor`) form the Adjust pipeline over BGRA byte arrays. Masked `PhotoAdjustmentLayer`s and `CubeLut` build on it. `RetouchProcessor` (per-face settings) and `RefineProcessor` (sharpen, noise, grain, dust) are separate. Processing order is **Retouch → color recipe → Refine**, and live preview skips Retouch and Refine. **Match Look's output is an editable `ColorRecipe`, like Capture One's Match Look; never a black-box LUT.** `ReferenceMatchEngine` (1–8 references; quantiles + per-luminance-band chroma "look-gram"; strict skin-colour guard) and `PairedMatchEngine` (same-scene pairs; also owns the shared coordinate-descent `Optimize` and parameter list, including Color Balance wheels) *fit recipe parameters* through `ColorProcessor`. A `.cube` is produced only *from* a recipe by `RecipeLutBaker` (Core) + `CubeLutFile` (Infrastructure); spatial tools are excluded and reported. `MatchRecipeComposer` scales a fitted recipe by Match Impact (0–100). It keeps the exposure and white-balance normalization at full strength, and can return the look as a masked layer. Print layout (`PrintLayout`, `PrinterConfiguration`, `TemplateSlotDetector`, `TemplateAperture`) is also pure Core.
- **Infrastructure** holds `SqliteBoothStore` (WAL, receipt uniqueness), `SimulatedDevices`, and the JSON stores for operator state. Those stores default to `%LOCALAPPDATA%/FotoAutomat/Operator/*.json` (and `active.fotolayout`), and their constructors accept a path override that tests use. `LightroomPresetImporter` parses XMP and CUBE files. The simulator DB lives in `%LOCALAPPDATA%/FotoAutomat/Simulator`. Drafts are kept separate from published presets, so saving one preset must not publish another draft.
- **Desktop** (`net10.0-windows10.0.19041.0`; the WinRT target is needed for `WindowsFaceDetection`) holds the windows, editor panels (`*EditorPanel`, `*PreviewSession`), and layout editor ViewModels. Mode is selected in `App.xaml.cs` from command-line flags. A mutex blocks a second instance in the same login session. `MatchImageReader` normalizes EXIF orientation and color profile and downsizes images to a 960 px long edge before matching.

**Python data tooling (outside the .NET build):** `tools/MatchLookData/` holds dataset inventory/selection, synthetic look generation (Phase B), skin/face region masks and booth capture intake (index in its `README.md`). The CanonCGT/LUT-generation research scripts were removed 14/10/2026 (backup zip in `.local/archive/`); their reports remain in `docs/ai-match-look/` as history. Start from `docs/ai-match-look/match-look-direction-2026-10-14.md`. Python envs, weights and datasets live in the git-ignored `.local/`; none of it is wired into the app.

## Docs to consult by task

The feature docs record exactly what is implemented versus planned. Update them along with behavior changes:
- Adjust: `docs/adjust-tools.md`, `docs/adjust-scope.md`
- Retouch: `docs/retouch-tools.md`
- Refine: `docs/refine-tools.md`
- Match Look: `docs/ai-match-look/match-look-direction-2026-10-14.md` (product definition), `docs/reference-match.md`, `docs/lut-export.md`, `docs/paired-match.md`, `docs/match-look-design.md`
- Presets: `docs/lightroom-presets.md`
- Customer UI: `docs/desktop-ui.md`
- Operator UI: `docs/operator-ui.md`
- Hardware: `docs/hardware-validation.md`; record SDK compatibility evidence there
- Architecture and production boundaries: `docs/architecture.md`
