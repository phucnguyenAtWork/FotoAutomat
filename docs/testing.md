# Testing and harnesses

## Two harnesses

The **agent harness** is `AGENTS.md`, linked engineering docs, clear module boundaries, pinned dependencies, and repeatable verification commands. It tells coding agents what is real, what remains planned, and where physical side effects are prohibited.

The **test harness** is `tools/FotoAutomat.Harness` plus `tests/FotoAutomat.Tests`. It runs the shared workflow with SQLite and deterministic fake devices. No customer files, camera, printer, money acceptor, or network service are needed after dependency restore.

## Verification commands

PowerShell, from the repo root:

```powershell
./scripts/verify.ps1
```

The script selects the full solution on Windows and the portable solution elsewhere. PowerShell is optional on macOS and Linux; the equivalent commands are:

```sh
dotnet restore FotoAutomat.Portable.slnx --locked-mode
dotnet format FotoAutomat.Portable.slnx --verify-no-changes --no-restore
dotnet build FotoAutomat.Portable.slnx --configuration Release --no-restore
dotnet test tests/FotoAutomat.Tests --configuration Release --no-build --no-restore
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build
```

Use `FotoAutomat.slnx` in the first three commands to include WPF compilation. To apply formatting intentionally, omit `--verify-no-changes` from `dotnet format`.

## Automated coverage

Tests cover receipt replay across database reopen, rejecting cross session receipt reuse, atomic rejected payment behavior, capture authorization, known camera failure and retry, uncertain print outcomes, duplicate print prevention, interrupted operation recovery, stale writes, concurrent print requests, print timeout, and cancellation before submission.

The harness runs four visible scenarios: `happy`, `camera-failure`, `print-unknown`, and `restart-recovery`. It emits one JSON result per scenario and exits nonzero on failure. Invalid CLI arguments return exit code 2.

Reopening a database simulates process recovery. It does not test physical power loss, filesystem corruption, or hardware behavior. Test those on a representative booth.

## CI

`.github/workflows/ci.yml` restores locked dependencies, verifies formatting, builds, runs tests and harness scenarios on Windows and Linux. It uploads test results and publishes an unsigned, framework dependent Windows evaluation bundle. Publication is not an installer, signature, deployment, or production release.

Changes under `.agents/skills` do not need application linting, but normal application checks still run. Dependabot tracks NuGet and GitHub Actions updates. Review updates and their licenses before merging.

## Customer UI preview checks

Portable tests cover both payment branches, six capture samples before selection, four filled slots before printing, print completion, four retakes, clearing slots on retake, preserving choices on back navigation, and clearing choices on a new session. These tests verify UI state, not WPF rendering or device behavior.

Run the customer UI on Windows and compare all 11 reference pages in `docs/screenshot/FotoAutomat.pdf`. Verify pointer and keyboard selection, disabled action states, F11/Escape, the persistent simulation notice, and 100%, 125%, 150% display scaling.

## Windows desktop checks

The following checks refer to the durable simulator, opened with `--simulator`.

Before marking the desktop runtime verified:

1. Build and run on the intended Windows version.
2. Complete the demo and confirm the image preview and button states.
3. Restart during an intermediate session and confirm restored state.
4. Open a second instance and confirm it is refused within the same login session.
5. Check touch targets, display scaling, keyboard navigation, and error visibility.
6. Run the [hardware plan](hardware-validation.md) separately before enabling any real device.

No automated WPF UI test is currently configured. CI compilation and portable tests do not replace these checks.
