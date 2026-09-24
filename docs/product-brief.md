# FotoAutomat

Status: product requirements captured; C#/.NET 10 with WPF selected for the booth.

## Purpose

FotoAutomat is commercial software for unattended photo booths running on Windows computers. Each booth connects a camera, a touchscreen, a money acceptor, and a DNP DS620A printer. Operators manage multiple booths.

## Customer journey

The requested photo journey is:

1. Choose a color look.
2. Take photos.
3. Apply beauty adjustments after capture.
4. Select a frame.
5. Print and receive digital files through a QR code.

Payment is part of the product. Its position in this journey, pricing, change handling, refund policy, and recovery after an interrupted paid session are not yet decided.

## Operator capabilities

Operators create color recipes from reference images using AI, then refine those recipes. The product needs synchronization and software updates across multiple booth computers.

AI recipe creation and customer photo processing are different operations. The model provider, execution location, processing pipeline, and publication workflow are not selected yet.

## Commercial distribution

The booth is a separately distributed Windows application for customers who rent or purchase usage rights. A customer web portal will manage booths and paid services. FotoAutomat staff need a separate permission scope for fleet operations and commercial administration. Subscription terms, perpetual entitlements, update eligibility, pricing, and cloud service limits remain open decisions in docs/ba/decisions.md.

## Hardware candidates

| Component | Current requirement | Validation status |
| --- | --- | --- |
| Operating system | Windows on each booth computer | Exact edition and supported versions to select |
| Camera | Fujifilm X-H2, Canon EOS R50, or Canon EOS R100 | Candidates only, physical integration not tested |
| Printer | DNP DS620A | Driver, output settings, status reporting, and physical output to test |
| Input | Touchscreen | Resolution, orientation, and touch behavior to select |
| Money acceptor | Required | Model, protocol, denomination support, and recovery behavior unknown |
| Compute | Low specification office computer | Exact CPU, memory, GPU, storage, and thermal limits unknown; dedicated GPU cannot be assumed |

## Confirmed offline requirement

When Internet access is unavailable, the booth must continue accepting payment, capturing photos, processing images, and printing. Fleet synchronization and digital upload wait for connectivity to return. A public download must not be presented as available until upload succeeds.

Design for low specification office computers. No exact minimum specification or performance guarantee has been established. Measure resource use and end to end latency on a representative Windows machine before selecting minimum supported hardware.

## Repository setup scope

The requested foundation includes:

* An application framework and documented architecture.
* A harness for coding agents with clear entry points and verification commands.
* Automated tests and continuous integration.
* Developer, hardware integration, and operational documentation.

The owner selected Jira project SCRUM for work tracking. BA screen definitions and commercial platform requirements are in docs/ba/. GitHub Issues are not used.

## Decisions still needed

* Recovery and customer messaging for digital delivery delayed by an Internet outage.
* Hardware access and SDK distribution permissions.
* Device provisioning, operator authentication, and remote management scope.
* Customer photo retention, deletion, and download access rules.
* Storage and hosting providers for synchronization and digital delivery.
* Installer, signing, rollout, rollback, and supported Windows versions.

## Current implementation status

The repository contains a WPF simulator shell, a shared session workflow, SQLite persistence, simulated devices, a command line harness, and automated tests. It does not yet contain real device adapters, AI processing, real payment handling, physical printing, QR delivery, fleet synchronization, or an updater.

This document records product intent. It is not a claim that any candidate device or production feature has been validated.
