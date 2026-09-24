# Security and privacy

## Current boundaries

The foundation uses only simulated devices and synthetic images. It has no network listener, cloud credentials, public download endpoint, administrator authentication, or remote command channel. Those absences are implementation status, not a complete production security design.

Agent and test tasks must not operate real payment hardware or printers unless the owner explicitly authorizes the physical test. Vendor SDKs and credentials belong outside the repository.

## Requirements for production design

| Area | Required design work |
| --- | --- |
| Customer photos | Storage access rules, retention, deletion, backup interaction, and previous session isolation |
| QR downloads | Unguessable access tokens, expiry policy, private storage, deletion behavior, and access limits |
| Device identity | Per booth credentials, secure provisioning, credential rotation, and revocation |
| Administration | Authenticated operators, authorization boundaries, audit events, and locked customer mode |
| Software updates | Signed packages, verification, controlled rollout, and compatible rollback |
| Local files | Validate image inputs, bound decoding resources, separate originals and outputs, and restrict write paths |
| External SDKs | Verify source, version, licenses, native dependency integrity, and failure isolation |
| Payment evidence | Stable event identity where supported, durable records, and operator reconciliation of ambiguity |

Do not put a cloud administrator key into a booth binary. Do not load or execute arbitrary code from a color recipe or frame package. Choose the recipe and frame data formats before implementing remote publication.

## Configuration

There are no required secrets or environment variables for the simulator. `.gitignore` excludes local environment files and runtime data. This is not a substitute for access controls on a deployed Windows machine.

Security controls and operational procedures above are not claimed as implemented. Record their acceptance evidence as each production feature is introduced.
