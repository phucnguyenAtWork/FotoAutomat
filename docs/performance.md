# Performance validation

Status: requirements and measurement plan. No benchmark results yet.

## Product constraint

FotoAutomat should run on a low specification office computer at a Windows photo booth. Do not assume a dedicated GPU. Exact minimum hardware is not selected.

## Proposed approach

* Create AI color recipes on the administration side, then publish a versioned recipe for deterministic application at a booth.
* Keep capture, approved color processing, beauty adjustments, frame composition, and printing available without Internet access.
* Offer a processing path that works on CPU. Do not require a large generative model to run during a paid booth session.
* Keep originals on disk and use smaller previews for the UI. Avoid keeping many decoded full resolution images in memory.
* Bound worker concurrency and queue sizes. Prioritize the active customer session over uploads and maintenance work.
* Avoid decoding and transforming photos on the UI thread.
* Define one color management path, including input profile, recipe representation, export profile, and printer settings. A reference photo alone is not a calibrated color specification.

These are proposals for implementation. They do not establish the quality or speed of a beauty model or image library.

## Measurements

Record hardware, operating system, power settings, application version, image resolution, image format, and each processing option alongside results.

| Measurement | Purpose |
| --- | --- |
| Cold startup and time until devices are ready | Customer and operator wait |
| Idle CPU and private working set | Background cost |
| Live view latency and dropped frames | Touchscreen preview quality |
| Capture trigger to downloaded original | Camera path latency |
| Original to processed preview | Time before customer can choose adjustments |
| Original to final print image | Processing and composition cost |
| Submission to physical print completion | Actual fulfillment time |
| Peak working set during a session | Memory pressure |
| Consecutive sessions and long idle periods | Leaks, thermal throttling, and recovery |
| Behavior while uploads are pending | Competition between local work and synchronization |

Set numeric acceptance budgets after the first representative hardware run. Report distributions and slow cases, not only averages. Store measured results separately from targets.
