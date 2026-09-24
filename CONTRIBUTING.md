# Contributing

Start with [development](docs/development.md), [architecture](docs/architecture.md), and [testing](docs/testing.md).

Keep each change focused on an observable outcome. Put workflow rules in Core, storage and device details in Infrastructure, and presentation in Desktop. Preserve failure behavior as carefully as the happy path.

Before proposing a change, run the verification commands, add meaningful tests for changed behavior, and update the relevant documentation. Record Windows checks separately from portable tests. State which hardware was actually used and which results came from simulators.

Do not include customer photos, credentials, runtime databases, vendor SDK archives, or build output. Review dependency licenses before introducing packages into this commercial product.

A pull request should explain the problem, resulting behavior, validation, and any recovery or data compatibility implications. Work is tracked in Jira project SCRUM. Link the relevant Jira item when one exists; BA references live in docs/ba/.

Only a separately approved release process may sign, deploy, or update booth software. The current CI bundle is for evaluation.
