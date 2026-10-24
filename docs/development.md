# Development

## Prerequisites

Install .NET SDK 10.0.401 or a compatible patch in the same SDK feature band, as specified by `global.json`. A runtime alone cannot build the repository. No Node, Python, Docker, cloud account, or manufacturer SDK is required for the current foundation.

Use Windows for the WPF UI and hardware work. Use an editor with C# support. macOS and Linux can build and test the portable solution. A cross build of WPF checks compilation only, not desktop behavior.

The desktop targets `net10.0-windows10.0.19041.0` for local WinRT face detection: Windows 10 build 19041 or later. The Windows SDK .NET reference assemblies are restored with the project; the Core and Infrastructure targets remain portable. See [Retouch](retouch-tools.md) for supported tools and manual face selection fallback.

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
dotnet run --project src/FotoAutomat.Desktop -- --workflow-simulator
```

In that simulator, use **New demo session**, **Simulate payment**, **Capture test image**, and **Simulate print**. The image is a generated color gradient. There is no AI processing or real money handling.

## Local state

Use `--admin` to open the [operator workspace](operator-ui.md). It stores operator draft notes separately under `%LOCALAPPDATA%/FotoAutomat/Operator`; these values do not override customer preview settings.

The customer UI preview holds its state only in memory and clears it on a new session. Settings are deferred. The durable simulator stores its SQLite database and synthetic captures under `%LOCALAPPDATA%/FotoAutomat/Simulator`. Restarting loads the latest session. Interrupted capture or print is marked `NeedsReview`. Starting a new demo after review preserves the old session record; it does not resolve a real payment or print.

The desktop mutex prevents another simulator window within the same Windows login session. This is not a production guarantee across multiple Windows user sessions. Production device ownership and recovery require a separate integration design.

The current database is a disposable development schema. It is not a migrated production ledger. Do not attach it to live hardware or use it for real sales.

## Troubleshooting

### C# Dev Kit reports missing `_wpftmp.csproj` projects

WPF generates temporary projects during markup compilation and removes them after use. These are not solution projects: `FotoAutomat.slnx` contains five real projects. If C# Dev Kit retains temporary project entries and reports them missing, reload the VS Code window (`Developer: Reload Window`) and open `FotoAutomat.slnx` in Solution Explorer. Workspace settings select that solution explicitly and exclude WPF temporary files from the general VS Code file watcher; the language server may have its own watchers, so this is not a guaranteed fix for every extension version.

Settings are provided in the repository `.vscode/settings.json`; for the current parent `Work` workspace there is also a local `.vscode/settings.json` selecting `FotoAutomat/FotoAutomat.slnx`. Do not recreate missing temporary project files or add them to the solution. A successful solution build verifies the real projects but does not prove that the running IDE has cleared its cached errors.

| Symptom | Check |
| --- | --- |
| SDK not found or incompatible | Install the SDK feature band in global.json; restart your terminal |
| Locked restore fails | Check central package changes and regenerate locks only for an intentional dependency update |
| WPF cannot run on macOS | Run portable tests and harness there; use Windows for the UI |
| Database is locked | Stop other tools using the simulator database; do not delete a database with a running owner |
| Session is NeedsReview | Inspect its last operation; do not manually mark a real print successful based on a spooler submission |
| Vendor SDK unavailable | Continue with the simulator and record hardware validation as pending |

Do not commit databases, generated photos, manufacturer binaries, or secrets. Existing `.agents/skills` are repository tooling; their contents are not application code.
