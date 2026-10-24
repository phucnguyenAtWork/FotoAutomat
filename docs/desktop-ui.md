> Cập nhật UI/UX 02/10/2026: xem [tiến độ và giới hạn hiện tại](ui-progress.md). Luồng khách hiện có bước chọn ảnh và làm đẹp trước khi ghép khung.

# Desktop customer UI

## Scope

The customer screens originated from `docs/screenshot/FotoAutomat.pdf`. The reference brand is Foto Hunter, while the application remains FotoAutomat. The [operator workspace](operator-ui.md), opened with `--admin`, now saves countdown settings used by newly opened customer windows.

This is an interactive UI prototype. Prices, filter names and packages are sample values. Capture timing follows the confirmed seven-minute policy. No real payment, camera capture, image processing, printing, upload, video or QR service is connected. Preview images are synthetic numbered tiles.

## Flow and values

Welcome → filter → print package → payment method → QR or cash → instructions → timed capture → favorite photos → frame and slot assignment → printing → delivery → new session.

Capture starts when the customer presses Ready and lasts the configured duration (420 seconds by default), including countdown and review time. The session clock is hidden until 300 seconds remain. Before each photo, countdown is 3, 5 or 10 seconds. The numbered sample is then shown for the configured review duration (7 seconds by default), followed automatically by the next countdown. There is no fixed capture count or retake/restart action during this phase.

Expiry during countdown finishes that countdown and one final sample before going directly to selection. Expiry during review goes straight to selection. Desktop timing uses monotonic elapsed time; delayed callbacks do not invent missed photos or extend the session deadline. Real SDK capture completion and durable recovery are not implemented by this preview.

Customers shortlist any captured samples before choosing a three- or four-slot sample frame. A favorite may fill multiple slots; all slots must be filled before simulated printing. Changing favorites clears assignments. An active admin layout replaces the sample frame choices when the customer window opens. Its geometry and PNG artwork are shown during assignment. Slots with the same photo number receive the same chosen photo. Artwork should have transparent photo openings. Output remains simulated; no production print image is rendered. Packages determine print quantity, not the number of camera exposures. A new session clears selections and timer state.

The countdown setting is persisted to `%LOCALAPPDATA%/FotoAutomat/Operator/capture.json`. Close and reopen an existing customer window after saving to load the new setting. Session duration is configurable from 1 to 60 minutes and review duration from 1 to 60 seconds, defaulting to 7 each. The warning remains at five minutes; shorter sessions show the clock immediately. All three settings support Save and Cancel and are loaded when opening the customer window.

## Design

Warm cream background, brown serif headings, rose accents, white rounded selection cards, large bottom actions and small progress marks. The delivery heading wraps cleanly instead of overlapping as it does on reference page 10. Native WPF controls provide focus and keyboard navigation. Shared visual tokens live in `src/FotoAutomat.Desktop/CustomerTheme.xaml`.

The customer canvas follows the reference 16:9 composition. A Viewbox scales it within the available window. Validate text and touch targets at the actual Windows display scaling before kiosk deployment. A persistent prototype notice sits outside the customer canvas. Full screen is available with F11; Escape returns to the normal window.

## Run and verify

`dotnet run --project src/FotoAutomat.Desktop` opens the customer prototype. Append `-- --simulator` to open the existing durable workflow simulator instead.

On Windows, check both payment branches, countdown/review cycles, the five-minute warning, expiry boundaries, the scrolling gallery, favorites, both frame sizes, print progress, delivery and a fresh session. Check keyboard focus and 100%, 125%, 150% display scaling. A cross build on macOS does not verify WPF rendering or touch interaction.

## Máy ảnh trong giao diện khách (23/10/2026)

- Giao diện khách tự dùng máy ảnh theo `camera-setup.json`: Fujifilm mở `FujifilmCameraConnection` (SDK đã lưu hoặc tự tìm). Canon/Nikon chưa có adapter: booth chạy dạng xem trước và thanh dưới ghi "Chưa có adapter …"; không tự chuyển sang máy ảnh mô phỏng. Ảnh gốc lưu ở `<Dữ liệu booth>/Sessions`.
- Kết nối khi mở booth. "Chạm để bắt đầu" bị khóa và hiện "Đang chuẩn bị máy ảnh…" cho tới khi kết nối xong; lỗi hiện "Máy ảnh chưa sẵn sàng. Vui lòng liên hệ nhân viên." và thử lại mỗi 5 giây ở màn chào. Chi tiết lỗi ghi ở thanh dưới.
- Nút Khởi động giao diện booth ngắt máy ảnh đang mở ở trang Máy ảnh trước khi mở giao diện khách (một kết nối USB một chủ).
- Chọn bộ lọc: mỗi thẻ là live view với bộ lọc đó (320 px, khoảng 3 khung/giây, xoay/lật theo cấu hình); chưa có máy ảnh thì dùng ảnh mẫu. Thẻ cuộn ngang bằng cảm ứng, tiêu đề không bị che.
- Hướng dẫn chụp: khung live view với bộ lọc đã chọn để khách kiểm tra khung hình trước khi bấm Sẵn sàng.
- Phát triển: `--simulated-camera ảnh.jpg` dùng ảnh đó làm live view và ảnh chụp (`SimulatedCameraConnection`), không chạm USB/SDK.
- Mở khóa PIN chỉ hiện chấm cho số đã nhập và bật nút xác nhận từ số đầu tiên, không lộ độ dài PIN.
- Chưa thử với máy Fujifilm thật trong lượt này; Canon cần adapter EDSDK riêng.

## Luồng khách: chế độ chụp, màn chờ, voucher, khuôn chụp (23/10/2026)

- Chọn chế độ chụp: mỗi thẻ là ảnh xem trước của bộ lọc đã phát hành, tiêu đề và mô tả. Tiêu đề/mô tả đặt ở Màn hình & thương hiệu → Thẻ chế độ chụp (lưu `mode-cards.json`, gắn PresetId); để trống thì dùng tên bộ lọc. Không dùng live view ở màn này.
- Bước Hướng dẫn được thay bằng màn chờ 4 giây (tấm ảnh động, số ảnh, thời gian xem lại, số lần chụp lại). Hết giờ tự vào chụp khi máy ảnh sẵn sàng; nếu máy ảnh lỗi thì giữ màn chờ với lời nhắn nhân viên và thử lại, không có nút quay lại.
- Chụp lại: Cài đặt chế độ chụp → Số lần chụp lại (0–5, mặc định 2, `capture.json` cũ vẫn đọc được). Khi đang xem lại ảnh vừa chụp có nút "Chụp lại · còn N"; ảnh gốc và biên nhận trên đĩa giữ nguyên.
- Màn chụp: live view hiện đủ khung cảm biến; phần ngoài vùng sẽ in (tỉ lệ ô ảnh đầu tiên của bố cục đang dùng, căn giữa như khi ghép ảnh vào khung) được phủ tối, có viền khuôn. Không có bố cục thì không có khuôn. Ảnh thật đang xem lại không bị che chữ.
- Thanh toán: nhập voucher bằng bàn phím số (dùng chung với hộp PIN), số tiền tính lại theo khổ, voucher miễn phí bỏ qua tiền mặt/QR. Mã thường 6 chữ số dùng một lần; mã nhân viên dùng không giới hạn và đếm lượt. Lượt dùng chỉ ghi khi khách qua bước thanh toán; 5 lần nhập sai liên tiếp khóa 30 giây. Quản trị tạo/hủy mã ở Bảng giá & thanh toán → Voucher (`vouchers.json`).
- Tiền mặt và QR vẫn là mô phỏng; chưa có nhà cung cấp thanh toán hay máy nhận tiền.
- Cập nhật: màn chờ hiện một tấm ảnh trong khung ảnh lấy liền kèm "N ảnh · chụp lại tối đa M lần". Ảnh đổi ở Màn hình & thương hiệu → Ảnh màn chờ (PNG/JPEG ≤ 10 MB); để trống thì dùng ảnh xem trước của chế độ khách đã chọn. Màn chụp dùng toàn khung màn hình (ẩn header và tiến trình). Dòng voucher ở thanh toán chỉ ghi mức giảm, không hiện mã.
- Cập nhật: ô voucher hiện mã đang áp dụng với nhãn "✓ Đã áp dụng" (viền màu nhấn). Màn chụp/xem lại: khoảng trống quanh ảnh là nền Ink, khung camera có viền mảnh, phần ảnh không được in phủ tối và gạch chéo. Nút xóa ảnh ghi "Chụp lại – còn N shots". Quản trị: thanh trạng thái dưới cùng chỉ hiện khi có thông báo, không còn câu hướng dẫn mặc định.

## Phiên khách và phục hồi (24/10/2026)

- Một nguồn dữ liệu: `Operator/booth.db` (SQLite, WAL, synchronous=FULL) gồm phiên, giao dịch, từng shot, lựa chọn ảnh/khung, nhật ký chuyển trạng thái và voucher. `vouchers.json` cũ được nhập một lần rồi đổi tên `.imported`.
- Phiên chỉ được tạo khi khách bấm đã thanh toán (tiền mặt/QR, vẫn mô phỏng) hoặc Bắt đầu chụp bằng voucher miễn phí; tạo phiên, ghi giao dịch và trừ voucher nằm trong một transaction. Phiên lưu ảnh chụp cấu hình lúc trả tiền (khổ, số tờ, số shot, số lần chụp lại, bộ lọc và công thức, layout không kèm ảnh khung, cấu hình chụp).
- Chọn khổ (24/10): Khổ nhỏ 2 × 6 hoặc Khổ lớn 4 × 6, cùng màn chọn số tấm in. 2 × 6 chỉ 2/4/6/8 (in hai dải một tờ rồi cắt); 4 × 6 từ 2 đến 8. Đổi sang 2 × 6 khi đang chọn số lẻ thì làm tròn lên. Khổ chưa có khung phát hành thì không chọn được; chưa phát hành khung nào thì dùng khung mẫu. Giá vẫn theo khổ, chưa tính theo số tấm. Snapshot phiên lưu số tấm và danh sách khung của khổ; lựa chọn lưu vị trí khung khách chọn. Khuôn chụp theo khung đầu tiên của khổ (khung chọn sau khi chụp).
- Bước Khung: nếu khổ có nhiều khung phát hành, khách chọn khung bằng nút tên khung phía trên.
- Màn In (24/10): có ảnh camera thì tạo file khung và tờ in thật từ ảnh gốc theo chế độ của phiên (PhotoProcessingService), thanh tiến độ theo job; lỗi thì báo liên hệ nhân viên và phiên chuyển Cần kiểm tra. Hoàn tất thì ảnh full độ phân giải cho QR chạy nền. Chưa gửi lệnh in thật.
- Tính năng theo chế độ (24/10): Trang Chế độ cho khách (nhóm Bộ màu) có công tắc LUT / Adjust / Blur & Glow / Retouch / Refine. Chưa cấu hình = LUT + Adjust. Ảnh thẻ chế độ, ảnh màn chờ, live view và ảnh vừa chụp chỉ áp tính năng đang bật; Lưu cài đặt thì ảnh thẻ render lại.
- Trước khi thanh toán (Chọn chế độ → Chọn khổ → Thanh toán/QR/Tiền mặt): 120 giây không chạm thì quay về màn chào, không ghi gì.
- Mỗi shot: Requested (trước khi bấm máy, cùng shotId với tệp intent/receipt của máy ảnh) → Received → Kept (hết thời gian xem lại) hoặc Discarded (nút X). Tệp gốc không bị xóa.
- Trạng thái phiên: Paid → Capturing → Selecting → Framing → Completed; NeedsReview khi lỗi máy ảnh, đóng booth hoặc app tắt giữa chừng (phục hồi lúc khởi động); Compensated khi nhân viên cấp voucher bù.
- Phiên chụp & thư viện: danh sách phiên từ booth.db (trạng thái, thực thu, phương thức/mã, số ảnh giữ/xóa). Phiên "Cần kiểm tra" có nút Cấp voucher bù: tạo mã miễn phí dùng một lần, hiện mã to cho nhân viên đọc; phiên mới dùng mã lưu liên kết tới phiên gốc.
- Tổng quan/doanh thu đọc booth.db: doanh thu là thực thu của phiên đã thanh toán, số tờ chỉ tính phiên hoàn tất. Chưa có xuất file in, in thật, thanh toán thật.
