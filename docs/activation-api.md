# Brand activation proposal

This is a proposed architecture, not an implemented licensing service.

The general settings API section shows disconnected status. Never put a server signing secret or key-generation secret into the desktop client.

## Three separate credentials

- Operator PIN: local customer-screen exit gate. It is not a Windows kiosk boundary and cannot prevent a Windows administrator from terminating the process or modifying its files.
- Revenue password: separate local permission for viewing financial metrics.
- Activation key: issued by the business to authorize a brand and a limited number of booth installations.

## Required services

1. An HTTPS activation service and database with brands, licenses, installations, expiry, permitted features, activation limits and revocation state.
2. An authenticated owner portal for creating keys, assigning brand/logo/theme, extending validity and releasing old installations. Do not expose key issuance anonymously.
3. A signing key stored only on the server. The desktop holds the public verification key.
4. Activation accepts the entered key and an installation identifier. The server validates limits and returns a signed entitlement containing brand ID, license ID, installation ID, features, validity and version.
5. Desktop stores that signed entitlement locally, verifies signature and installation binding, and periodically refreshes online. Define an offline allowance before implementation; photography, local processing and printing must continue without Internet within that allowance. Never interrupt an active paid session due to a refresh failure.
6. Deactivation/replacement releases a machine slot with an audited owner action. Revocation cannot be immediate on a machine that remains offline.

Suggested endpoints: POST /v1/activations, POST /v1/activations/refresh, POST /v1/activations/release. Owner-only issuance endpoints belong to a separate administrative surface. Use request bodies or authorization headers for secrets, never URL query parameters. Rate-limit activation attempts, store only key hashes where feasible, and avoid logging credentials.

Brand identity should be a stable ID, not the logo filename. Theme packages can reference a version and content hash, while shared booth workflow remains independent of the theme.

The local device-control API (capture/status/events) is a separate future integration. Default to disabled and loopback-only, require a scoped credential, and do not provide arbitrary executable or shell-command endpoints. Event triggers and payment-provider integration are deferred; a mock API field must not imply a running service.

Session downloads require separate object storage, a customer gallery service, opaque per-session URLs, retention/deletion rules, and offline upload retry. Activation keys must never appear in customer QR links.

References: https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html and https://learn.microsoft.com/en-us/dotnet/standard/security/cryptographic-signatures
