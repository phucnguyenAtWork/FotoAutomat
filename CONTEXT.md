# Domain vocabulary

These names describe the confirmed product intent. The current code implements only a simulated session flow.

| Term | Meaning |
| --- | --- |
| Booth | One Windows computer with its camera, touchscreen, money acceptor, and printer |
| Customer session | One customer's photo workflow at a booth |
| Operator | A person managing booth configuration, content, or operations |
| Color recipe | A repeatable set of color adjustments authored from a reference and refined by an operator |
| Published recipe | A version of a recipe approved for customer use; publication is not implemented yet |
| Beauty adjustment | Processing applied after capture, separate from the selected color recipe |
| Frame | The chosen layout or artwork around the customer's photos |
| Print intent | A durable record that a specific output is about to be submitted |
| Print outcome | Evidence of what happened after submission; it can be unknown |
| Digital delivery | Giving the customer access to digital files through a QR linked download |
| Fleet | The collection of deployed booths managed together |
| Simulator | Development implementations that never accept physical money, operate a camera, or print |

Do not use a successful spooler submission as a synonym for a physically completed print. Do not call a placeholder recipe or simulated device a production integration.
