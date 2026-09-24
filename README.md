# FotoAutomat

Commercial Windows photobooth software, designed for low specification booth computers and local operation without Internet access.

**Current stage: simulator foundation.** The repository has an interactive WPF customer UI preview based on the supplied PDF, a durable simulated session workflow, a command line harness, and automated tests. Physical devices and commercial features are not integrated yet.

## Product direction

Operators create and refine color recipes from reference images. Customers choose a look, take photos, apply beauty adjustments, choose a frame, print, and receive digital files through a QR code. Booths will synchronize approved content and receive managed software updates.

Capture, local processing, and printing must continue during Internet outages. Uploads and fleet synchronization wait for connectivity. AI recipe generation belongs outside the booth's critical customer path.

## Start here

Install the **.NET SDK specified in [global.json](global.json)**. You need Windows to run the WPF desktop. Shared tests and the simulator harness also run on macOS and Linux.

```sh
dotnet restore FotoAutomat.slnx --locked-mode
dotnet build FotoAutomat.slnx --configuration Release --no-restore
dotnet test tests/FotoAutomat.Tests --configuration Release --no-build --no-restore
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build
```

Run the desktop on Windows:

```sh
dotnet run --project src/FotoAutomat.Desktop --configuration Release --no-build
```

For a development machine that does not need to build WPF, use `FotoAutomat.Portable.slnx` for restore and build. See [development](docs/development.md) and [testing](docs/testing.md) for details.

## What is ready

| Capability | Status |
| --- | --- |
| Customer UI preview, 11 reference screens | Implemented with sample data; Windows visual verification required |
| Durable workflow simulator | Available with `--simulator`; physical devices are not connected |
| SQLite session state and duplicate demo receipt handling | Implemented and covered by automated tests |
| Simulated capture and print | Implemented; no physical device calls |
| Interrupted or uncertain print handling | Held for review; no automatic reprint |
| Camera SDKs and DNP DS620A integration | Planned; hardware validation required |
| Real money acceptor and payment ledger | Planned |
| Color recipes, beauty processing, frames | Planned |
| QR delivery and fleet synchronization | Planned |
| Signed installer and managed updates | Planned |

A passing simulator is not evidence of hardware compatibility or commercial readiness.

## Repository map

```text
src/
  FotoAutomat.Core/             Session rules, workflow, device and storage interfaces
  FotoAutomat.Infrastructure/   SQLite and simulated device implementations
  FotoAutomat.Desktop/          Windows WPF simulator
tools/FotoAutomat.Harness/     Repeatable command line scenarios
tests/FotoAutomat.Tests/       Workflow and persistence tests
docs/                         Product, development, hardware and operations docs
scripts/                      Verification entry point
.github/workflows/            Windows and Linux CI
```

## Documentation

* [Documentation index](docs/README.md)
* [Product brief](docs/product-brief.md)
* [Architecture](docs/architecture.md)
* [Customer desktop UI and reference flow](docs/desktop-ui.md)
* [BA screens and Jira backlog](docs/ba/README.md)
* [Agent instructions](AGENTS.md)
* [Contribution guide](CONTRIBUTING.md)

The project is commercial. No open source license has been selected or granted by this repository setup. Third party material retains its own license notices.
