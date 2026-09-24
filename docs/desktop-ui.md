# Desktop customer UI

## Scope

The owner selected customer screens only on 2026-09-25. Settings and configuration persistence are deferred. The visual and workflow reference is `docs/screenshot/FotoAutomat.pdf`, all 11 pages. The reference brand is Foto Hunter, while the application remains FotoAutomat.

This is an interactive UI prototype. Prices, filter names, packages, retake limit and copy are sample values from the PDF. No real payment, camera capture, image processing, printing, upload, video or QR service is connected. Preview images are synthetic numbered tiles.

## Flow and values

Welcome → filter → layout and quantity → payment method → QR or cash → instructions → capture → assign photos to slots → printing → delivery → new session.

The source shows three filters, two layouts, quantity/price pairs 2/80,000, 4/140,000, 6/210,000, 8/280,000 VND, six captured photos, four frame slots and up to four retakes. Those quantities remain distinct. Countdown is a three second prototype default; printing is a short visual simulation. Neither is a production device timing policy. Cash and QR confirmation actions are visibly simulated. Going back before payment preserves selections; starting a new session clears them. Retaking clears assigned slots and generates a new set of synthetic tiles.

## Design

Warm cream background, brown serif headings, rose accents, white rounded selection cards, large bottom actions and small progress marks. The delivery heading wraps cleanly instead of overlapping as it does on reference page 10. Native WPF controls provide focus and keyboard navigation. Shared visual tokens live in `src/FotoAutomat.Desktop/CustomerTheme.xaml`.

The customer canvas follows the reference 16:9 composition. A Viewbox scales it within the available window. Validate text and touch targets at the actual Windows display scaling before kiosk deployment. A persistent prototype notice sits outside the customer canvas. Full screen is available with F11; Escape returns to the normal window.

## Run and verify

`dotnet run --project src/FotoAutomat.Desktop` opens the customer prototype. Append `-- --simulator` to open the existing durable workflow simulator instead.

On Windows, check both payment branches, selection feedback, countdown, six generated tiles, four slot assignment, the four retake limit, print progress, delivery and a fresh session. Check keyboard focus and 100%, 125%, 150% display scaling. A cross build on macOS does not verify WPF rendering or touch interaction.
