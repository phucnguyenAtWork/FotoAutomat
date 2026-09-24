# Operations

## Current simulator

Only development operation is available. The desktop loads the latest saved session, and its startup recovery marks interrupted capture or print as NeedsReview. It does not automatically resubmit an uncertain print.

A new demo session can be started after Completed or NeedsReview. The previous record remains in SQLite. There is no reconciliation screen yet, and starting a new demo is not a production recovery policy.

The CLI harness creates isolated data in a temporary directory and writes its result as JSON. Its output directory contains only synthetic test data. Preserve relevant output when investigating a failure.

## Production operating requirements

The following capabilities are planned and must be implemented and validated before unattended operation.

### Startup and readiness

Validate storage capacity, database compatibility, device connectivity, printer media, approved recipe availability, and whether an interrupted paid session needs attention. Do not accept payment while a required device cannot fulfill the session. The exact inhibition behavior depends on the money acceptor.

### During a session

Record payment evidence and state changes durably. Preserve original captures separately from derived images. Avoid applying content or software updates midway through a session. Distinguish device submission from confirmed completion.

### Recovery

After an uncertain payment or print, preserve evidence and request operator reconciliation. Do not guess that a timeout means no money was accepted or no photo was printed. Do not automatically refund money through hardware that cannot perform a refund.

Production single owner device locking, watchdog behavior, crash logging, operator authorization, backup, restore, and reconciliation are not implemented in this scaffold.

### Internet outage

Keep the booth's local customer workflow available. Queue upload and synchronization work durably. Bound retries and storage growth. Communicate digital delivery availability accurately; a public download cannot work until its file is accessible.

Decide with the product owner how long to retain originals, how customers retrieve delayed files, and when to stop accepting new sessions because disk space is insufficient. Do not invent retention periods or silently delete undelivered photos.

### Fleet updates

Use authenticated device provisioning, signed application packages, and staged rollout to test booths before general deployment. Verify package identity and integrity before installation. Apply updates only at an idle maintenance point. A rollback must remain compatible with stored data and recipe formats.

The CI evaluation bundle is unsigned and requires the .NET 10 Windows Desktop Runtime. It is not a commercial installer or an auto update package. Signing, rollout, rollback, and remote administration are not implemented.

### Observability

Future structured events should include booth identifier, session or job identifier, event type, application version, device adapter version, and error category. Exclude raw photos, credentials, and public download tokens from routine logs. Monitor repeated device failures, ambiguous print jobs, disk pressure, and delayed uploads.

The current CLI has structured scenario results; the desktop does not yet have a production log pipeline or fleet telemetry.
