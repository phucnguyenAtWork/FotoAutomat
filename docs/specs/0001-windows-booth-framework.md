# 0001. Windows booth framework

**Date**: 2026-09-24
**Status**: Accepted

## Summary

Use C# on .NET 10 with a native WPF interface for the Windows booth. The owner selected this framework after comparing it with React hosted in WebView2. Shared logic and the simulator harness remain independent of Windows UI so they can be tested on a developer's Mac.

## Context

FotoAutomat operates cameras, a money acceptor, and a printer on low specification office computers. It must keep accepting payment, capturing, processing, and printing without Internet access. Exact computer specifications and the money acceptor model remain unknown.

This record decides the booth framework only. It does not select a cloud provider, AI model, production payment protocol, or vendor SDK.

## Options considered

| Option | Benefit | Cost |
| --- | --- | --- |
| .NET and WPF | One application language for Windows UI and orchestration; access to native Windows integration | Desktop UI can only run on Windows; requires XAML skills |
| .NET and React in WebView2 | Web UI development on macOS; reusable web presentation work | Adds a browser runtime, a message bridge, and a second UI toolchain |
| Tauri and a native device component | Web presentation with a native host | Introduces Rust and vendor integration work without a confirmed platform requirement beyond Windows |

## Decision

Choose .NET 10 and WPF. The owner confirmed this choice on 2026-09-24. Acceptance records the framework decision, not production readiness.

## Rationale

The booth has one target operating system and constrained resources. A native UI avoids a browser dependency for this application. This is an architectural simplification, not a measured performance guarantee. Image processing and device latency must be profiled independently.

## Proposed stack

| Layer | Choice | Reason |
| --- | --- | --- |
| Language and runtime | C# and .NET 10 | A supported runtime for Windows integration and portable shared tests |
| Booth UI | WPF | Native Windows UI without an embedded browser |
| Local storage in the foundation | SQLite through Microsoft.Data.Sqlite | Durable simulator state without operating a separate database server |
| Hardware boundary | Small C# interfaces with simulator implementations | Run failure tests without vendor SDKs or physical side effects |
| Harness | .NET console executable and xUnit tests | Reproduce scenarios on macOS and Windows |
| CI | GitHub Actions on Windows and Linux | Build the actual Windows target and test the portable layers |

SQLite, xUnit, and CI are implementation choices for the requested foundation. They do not settle the production financial ledger or fleet service architecture.

## Consequences

* Windows is required for interactive WPF and physical device validation.
* Shared tests can run on macOS, but cannot certify Windows driver or printer behavior.
* Vendor SDK licensing and model compatibility remain separate work.
* AI recipe authoring should occur outside the booth. The booth should consume approved recipes and process photos locally. The concrete image pipeline remains to be selected.
* This repository starts with a simulator. It cannot accept real money or fulfill real orders.

## Follow-up

* Validate the camera SDKs, printer, and money acceptor on representative hardware.
* Select minimum supported Windows and computer specifications after measurement.
* Design production recovery, image processing, device provisioning, customer delivery, and fleet updates separately.
* Additional Agent Skills and MCP discovery is deferred at the owner's request.
