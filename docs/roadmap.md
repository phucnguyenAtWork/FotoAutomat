# Engineering roadmap

Work tracking uses Jira project SCRUM. The BA and backlog draft are in docs/ba/. This document records milestone intent, not a commitment that every product capability is already implemented.

## 1. Foundation

Set up the approved .NET and WPF framework, a durable simulator workflow, deterministic failure scenarios, agent guidance, portable tests, Windows build configuration, and CI. See [verification](verification.md) for checks actually run.

## 2. Hardware proof

Obtain a representative low specification Windows computer and the exact money acceptor model. Validate candidate camera SDKs and the DNP printing path using the [hardware test plan](hardware-validation.md). Record compatibility evidence and distribution terms before selecting production adapters.

Done when an application owned test can capture, download, print, detect failures, and reconnect on the chosen hardware, with documented limits.

## 3. First real local journey

Settle the payment protocol and business rules. Implement a durable paid session, a real capture, a basic deterministic image transformation, one frame, and one physical print. Add operator reconciliation and test interrupted operations. Measure performance before expanding processing features.

## 4. Image quality

Design versioned recipes and the admin creation and review workflow. Evaluate AI assistance outside the booth. Benchmark color and beauty processing on CPU, including preview and final print quality. Select models and libraries only after reviewing commercial usage terms and memory cost.

## 5. Digital delivery and fleet management

Select hosting and identity architecture. Implement durable uploads, private customer downloads, content synchronization, device health, provisioning, and access control. Test Internet outages and delayed delivery.

## 6. Controlled commercial deployment

Implement an installer, code signing, kiosk configuration, update rollout, rollback, migrations, backup and restore, retention, and operator runbooks. Run hardware endurance tests and a limited pilot before expanding the fleet.

## Decisions waiting for evidence

* Exact CPU, RAM, graphics capability, and Windows edition.
* Money acceptor model and protocol.
* Final camera and SDK entitlement.
* Pricing, change, refund, retake, cancellation, and reprint policies.
* Required paper sizes, cut modes, frame dimensions, and color calibration.
* Photo retention and delayed QR delivery behavior.
* AI recipe authoring and beauty model selection.
* Cloud provider, administrator access, and fleet rollout policy.
