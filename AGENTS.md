# Working on FotoAutomat

Read README.md, docs/development.md, and the documentation relevant to the task before editing.

## Product constraints

Windows booth application, C#/.NET 10 and WPF. Target low specification office computers. Capture, local image processing, and printing must work without Internet access. The current application is a simulator foundation, not a commercial booth release.

## Development rules

* Keep Core independent of WPF, SQLite, and manufacturer SDKs.
* Put storage and device implementations in Infrastructure. Keep UI code in Desktop.
* Test shared behavior through the CLI harness and automated tests. Run the desktop UI on Windows before approving a Windows release.
* Default to simulated devices. Never accept real money, print on physical hardware, or trigger a camera without explicit authorization for that integration test.
* Persist intent before any external side effect. An uncertain print outcome requires reconciliation, never an automatic duplicate submission.
* Do not commit customer photos, credentials, device identities, vendor SDK binaries, runtime databases, or generated build output.
* Do not assume a connected camera is SDK compatible. Record evidence in docs/hardware-validation.md.
* Keep status claims accurate. A Windows cross build on macOS is not a Windows runtime test.
* Update relevant docs with behavior and command changes. Prefer small complete changes and meaningful failure tests.

## Verification

See docs/testing.md for the authoritative commands. Run the portable tests and simulator scenarios for shared logic changes. Run the Windows build for desktop changes. Do not report CI as passed until its run has actually completed.

## CodeGraph

If .codegraph exists, use codegraph explore or codegraph node before searching for or reading code to understand it. If the index is unavailable or stale, report that and use source tools. Do not reindex without the user's request.

## Agent skills

### Issue tracker

Work is tracked in Jira project SCRUM. See docs/agents/issue-tracker.md.

### Domain docs

Single context, with root CONTEXT.md and docs/adr/ when needed. See docs/agents/domain.md.

### Workflow

Use only the installed skills relevant to the task. Skill availability is not a requirement to load or invoke every skill. Explicit user instructions take precedence over skill defaults.
