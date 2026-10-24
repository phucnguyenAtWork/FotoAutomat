# Documentation

Use this index to find the current source of truth. Distinguish implemented simulator behavior from proposed production capabilities.

| Document | Use it for |
| --- | --- |
| [BA pages và backlog Jira](ba/README.md) | Màn hình, luồng UI/UX và công việc cho sản phẩm thương mại |
| [Product brief](product-brief.md) | Confirmed product intent and open product decisions |
| [Framework decision](specs/0001-windows-booth-framework.md) | Why the booth uses .NET and WPF |
| [Architecture](architecture.md) | Current module boundaries and proposed production boundaries |
| [Development](development.md) | Prerequisites, commands, local data, and troubleshooting |
| [Testing](testing.md) | Agent/test harnesses, automated checks, and Windows validation |
| [Hardware validation](hardware-validation.md) | Physical test procedures and evidence requirements |
| [Hardware research](hardware-research.md) | Manufacturer evidence and compatibility unknowns |
| [Performance](performance.md) | Low specification hardware constraints and measurement plan |
| [Match Look in Adjust](reference-match.md) | Implemented reference matching to editable recipes: v4 Color Balance, multiple references, look-gram |
| [LUT export](lut-export.md) | Bake an edited recipe into a `.cube`; accuracy and tools that cannot be baked |
| [Match Look direction 14/10/2026](ai-match-look/match-look-direction-2026-10-14.md) | Parameters-first direction, what was kept from CanonCGT research, next steps |
| [AI Match Look research](ai-match-look/research-2026-10-05.md) | Primary sources, model candidates, licensing and integration proposal; not an implemented AI engine |
| [AI Match Look training plan](ai-match-look/training-plan.md) | Dataset collection, split, experiments, CPU booth constraints and training readiness |
| [CanonCGT implementation and manual workflow](ai-match-look/implementation-and-manual-workflow.md) | Ordered implementation, portrait/scene evaluation, percentage definitions and manual data preparation |
| [CanonCGT phase status](ai-match-look/phase-status.md) | Implemented inventory command, verification and remaining phase gates |
| [Operations](operations.md) | Simulator operation and future booth recovery requirements |
| [Security and privacy](security.md) | Device, customer photo, update, and agent boundaries |
| [Roadmap](roadmap.md) | Next engineering milestones, without an issue tracker |
| [Verification](verification.md) | Checks actually performed and checks still pending |
| [Domain documentation rules](agents/domain.md) | How agents read domain terms and decisions |

Update relevant documents when behavior changes. Keep one authoritative location for each rule; link to it elsewhere. Do not mark a planned integration as implemented because an interface or simulator exists.
