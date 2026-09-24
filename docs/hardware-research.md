# Hardware research

Checked on 2026-09-24 using manufacturer documentation. This is a compatibility investigation, not a physical test report.

## Fujifilm X-H2

Fujifilm lists X-H2 in its Camera Control SDK compatibility information and describes remote camera control and image transfer. The public SDK overview does not establish that every required live view operation is available. Business access, commercial distribution rights, supported firmware, and exact APIs still need confirmation with Fujifilm.

A compatible tethering utility is not evidence that FotoAutomat can embed or redistribute its behavior.

Source: [FUJIFILM Camera Control SDK](https://www.fujifilm-x.com/en-in/camera-control-sdk/).

## Canon EOS R50 and EOS R100

Model specific support for the required SDK operations was not verified during this research. Treat both as candidates, not supported devices. Obtain the current Canon SDK model list and applicable distribution terms, then test live view, capture, download, and reconnect through an application owned integration.

Source entry point: [Canon Developer Programme](https://developers.canon-europe.com/s/). The portal was not accessible to the research tool; this link is not evidence of model compatibility.

## DNP DS620A

The manufacturer describes DS620A as a USB photo printer suitable for booth and event use. Its public product information and downloads entry point do not by themselves establish a redistributable SDK contract or precise completion feedback.

Evaluate an application owned Windows printing path using the exact manufacturer driver. Determine whether the required sizes and cut modes work through that path and how device status can be obtained. Do not assume that the Hot Folder Print utility is an embeddable SDK.

Sources: [DS620A product](https://www.dnpphoto.com/products/printers/ds620a), [DNP downloads](https://www.dnpphoto.com/downloads).

## Framework evidence

[Microsoft's WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-9.0) confirms that WPF runs on Windows. [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) identifies .NET 10 as an LTS release supported through November 2028. The alternative investigated was [WPF hosting WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf).

These facts support the framework comparison. They do not establish which framework is faster on the target computer.

## Next evidence

Use the [hardware validation plan](hardware-validation.md) to record SDK access, exact device versions, executable test steps, observed results, and unresolved failures. Do not select a production camera based only on a marketing compatibility page.
