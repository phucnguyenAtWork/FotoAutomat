# Development

## Prerequisites

Install .NET SDK 10.0.401 or a compatible patch in the same SDK feature band, as specified by `global.json`. A runtime alone cannot build the repository. No Node, Python, Docker, cloud account, or manufacturer SDK is required for the current foundation.

Use Windows for the WPF UI and hardware work. Use an editor with C# support. macOS and Linux can build and test the portable solution. A cross build of WPF checks compilation only, not desktop behavior.

## Restore and build

From the repository root:

```sh
dotnet --version
dotnet restore FotoAutomat.Portable.slnx --locked-mode
dotnet build FotoAutomat.Portable.slnx --configuration Release --no-restore
```

On Windows, use `FotoAutomat.slnx` to include the desktop. The full solution also enables Windows targeting for compilation on other operating systems.

Dependencies are pinned centrally in `Directory.Packages.props`. Commit the `packages.lock.json` files. To intentionally update a dependency, edit its central version, restore without `--locked-mode`, review the changed locks, and run the verification suite. Do not solve restore failures by disabling locked mode in CI.

## Run

```sh
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build -- --list
dotnet run --project tools/FotoAutomat.Harness --configuration Release --no-build -- --scenario print-unknown
```

Each harness scenario uses its own generated directory under the operating system temporary directory. Its JSON output reports the path. It never opens a real camera, print queue, or money acceptor.

On Windows after building the full solution:

```sh
dotnet run --project src/FotoAutomat.Desktop --configuration Release --no-build
```

The default window is the customer UI preview. Follow the reference flow through both payment branches; all operations use sample data. F11 toggles full screen and Escape leaves it. See [customer UI](desktop-ui.md).

To open the original durable simulator instead:

```sh
dotnet run --project src/FotoAutomat.Desktop -- --simulator
```

In that simulator, use **New demo session**, **Simulate payment**, **Capture test image**, and **Simulate print**. The image is a generated color gradient. There is no AI processing or real money handling.

## Local state

The customer UI preview holds its state only in memory and clears it on a new session. Settings are deferred. The durable simulator stores its SQLite database and synthetic captures under `%LOCALAPPDATA%/FotoAutomat/Simulator`. Restarting loads the latest session. Interrupted capture or print is marked `NeedsReview`. Starting a new demo after review preserves the old session record; it does not resolve a real payment or print.

The desktop mutex prevents another simulator window within the same Windows login session. This is not a production guarantee across multiple Windows user sessions. Production device ownership and recovery require a separate integration design.

The current database is a disposable development schema. It is not a migrated production ledger. Do not attach it to live hardware or use it for real sales.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| SDK not found or incompatible | Install the SDK feature band in global.json; restart your terminal |
| Locked restore fails | Check central package changes and regenerate locks only for an intentional dependency update |
| WPF cannot run on macOS | Run portable tests and harness there; use Windows for the UI |
| Database is locked | Stop other tools using the simulator database; do not delete a database with a running owner |
| Session is NeedsReview | Inspect its last operation; do not manually mark a real print successful based on a spooler submission |
| Vendor SDK unavailable | Continue with the simulator and record hardware validation as pending |

Do not commit databases, generated photos, manufacturer binaries, or secrets. Existing `.agents/skills` are repository tooling; their contents are not application code.
