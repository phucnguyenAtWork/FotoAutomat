# Foundation verification

Date: 2026-09-24.
Environment: macOS on Apple Silicon, .NET SDK 10.0.401 installed temporarily under `/private/tmp/fotoautomat-dotnet` for this setup session.

## Checks performed

| Check | Result |
| --- | --- |
| Restore full solution with package lock files | Passed |
| Full Release build, including WPF Windows target | Passed, 0 warnings and 0 errors |
| dotnet format verification | Passed |
| Automated tests | 14 passed, 0 failed, 0 skipped |
| CLI happy scenario | Passed |
| CLI camera failure scenario | Passed |
| CLI unknown print scenario | Passed |
| CLI restart recovery scenario | Passed |
| JSON and dependency lock file parsing | Passed |
| CI and Dependabot YAML parsing | Passed |
| Internal documentation links | Passed, no missing targets |
| Independent framework document review | No material findings for the simulator foundation |

The test runner writes local TRX evidence to `artifacts/test-results/tests.trx`. Harness JSON output records each generated data directory. These generated files are ignored by Git.

The first sandboxed build stalled. The build completed with compiler servers disabled outside the tool sandbox, using `--disable-build-servers -m:1 -p:UseSharedCompilation=false`. This is a setup environment observation, not a claimed application defect.

The independent review covered the framework decision, product brief, architecture, and this verification record. It was a document review, not a separate code audit.

## Not performed

* Interactive Windows execution of the WPF window.
* Execution of the GitHub Actions workflow on hosted runners.
* Windows packaging and evaluation bundle startup.
* Real camera, money acceptor, or DNP printer tests.
* Low specification Windows performance or endurance measurements.
* Actual power loss, disk corruption, or production migration tests.
* AI image processing, QR delivery, synchronization, or signed update validation; those features are not implemented.

The PowerShell wrapper and CI configuration are provided. The equivalent build, format, test, and harness commands were exercised directly on macOS. Do not describe the Windows application or CI as verified until those environments have run them.

Install the SDK specified by `global.json` on each development machine. The temporary SDK used here is not a permanent developer installation.
