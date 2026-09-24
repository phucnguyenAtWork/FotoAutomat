# Hardware validation plan

Status: planned, no physical hardware validation performed.

## Evidence to record

For every run, record the date, operator, Windows edition and build, application commit, device model, firmware, driver or SDK version, connection type, power arrangement, test steps, observed result, and diagnostic log location. Use test images without customer data.

A successful test in a manufacturer's utility is useful evidence, but does not establish that the same operation is available through an SDK that FotoAutomat can distribute.

## Camera

Test each candidate separately. Do not transfer results between camera models.

* Confirm SDK access, supported model and firmware, process architecture, and distribution terms.
* Connect and disconnect without restarting Windows.
* Start and stop live view, measure delay, and verify orientation.
* Trigger capture and download the full resolution file.
* Confirm whether live view and capture require switching modes.
* Exercise focus, exposure, and file format controls required by the booth.
* Repeat captures during a sustained session and monitor temperature and power.
* Unplug USB during live view and during capture, then reconnect.
* Recover from SDK hangs, camera sleep, and camera power loss.

## DNP DS620A printer

* Install the official Windows driver and record its exact version.
* Validate the chosen paper sizes, cut settings, margins, orientation, and number of copies.
* Print a color test target and verify the intended color management path.
* Distinguish application submission, Windows spooler status, and physically completed output.
* Exercise out of paper, out of ribbon, printer cover open, USB loss, and power loss.
* Restart the application while a job is pending.
* Verify that recovery does not silently print the same paid order twice.
* Record what the available driver or SDK can actually report. Do not treat missing feedback as success.

## Money acceptor

Wait for the exact hardware model and protocol before writing a real adapter.

* Document accepted denominations, currency, enable and inhibit commands, and available acknowledgements.
* Test duplicate signals, partial messages, reconnects, and Windows port reassignment.
* Test whether the hardware reports retained credit after a restart.
* Interrupt power between receiving money and recording the transaction.
* Document how an operator resolves ambiguous credit and whether physical refunds are possible.

## Booth recovery

* Interrupt the application at each step of a paid session.
* Exercise disk full, device disconnect, Internet loss, and Windows restart.
* Verify that another customer cannot see photos from a previous session.
* Test touchscreen scaling and orientation on the actual display.
* Measure sustained capture, processing, and print times on the target computer.
* Verify recovery behavior before enabling unattended commercial operation.

## Fleet updates

* Test updates on a dedicated test booth before expanding a rollout.
* Exercise a download interruption, an invalid signature, and a failed startup.
* Verify that an active paid session prevents installation and restart.
* Verify compatibility between application versions, stored session data, and recipe versions before rollback.

These are acceptance targets for future implementation, not automated checks that currently exist.
