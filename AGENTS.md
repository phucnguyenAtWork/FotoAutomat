# Working on FotoAutomat

> Cập nhật quyết định 05/10/2026: người dùng chọn CanonCGT làm nền phát triển Match Look và thư viện Digital Camera Looks. Xem `docs/ai-match-look/data-readiness-2026-10-05.md`. Đây là quyết định hướng phát triển, chưa phải model đã tích hợp hoặc benchmark đạt.

Read README.md, docs/development.md, and the documentation relevant to the task before editing.

## Product constraints

Windows booth application, C#/.NET 10 and WPF. Target low specification office computers. Capture, local image processing, and printing must work without Internet access. The current application is a simulator foundation, not a commercial booth release.

## Mục đích và phạm vi sản phẩm — xác nhận ngày 28/09/2026

FotoAutomat hướng tới ứng dụng photobooth thương mại: quản trị tạo và phát hành bộ màu có thể dùng lại; khách chụp ảnh với màu đã chọn, làm đẹp, chọn ảnh/khung, thanh toán, in và nhận tệp qua QR. Giao diện quản trị và giao diện khách là các phần của sản phẩm; hoàn thiện UI không đồng nghĩa đã hoàn thiện xử lý ảnh, tích hợp thiết bị hoặc đủ điều kiện phát hành thương mại.

Đây là ngữ cảnh yêu cầu phải giữ xuyên suốt các tác vụ. Phân biệt rõ quyết định đã xác nhận, phương án đang nghiên cứu và tính năng đã triển khai. Không tự biến đề xuất thu hẹp MVP thành quyết định của người dùng. Người dùng đã nhắc định hướng sản phẩm gồm 3 phần; bảng dưới là lộ trình 7 phase, không phải định nghĩa thay thế cho 3 phần đó. Không tự suy diễn tên hoặc ranh giới 3 phần khi tài liệu chưa xác nhận.

### Phạm vi xử lý ảnh và AI

* **Adjust:** nghiên cứu chỉnh sáng, cân bằng trắng, tương phản, curves, nhóm màu, color balance và trắng đen.
  Phạm vi tham khảo bổ sung ngày 02/10/2026 gồm Histogram, Kelvin/Tint, Brightness, HDR, Levels, Curve theo kênh, Color Editor/HSL/Skin Tone, Color Balance, B&W mix/Split Tones, Clarity/Structure, Dehaze, Vignetting, Layers/Masks và Style Brushes. Xem `docs/adjust-scope.md`; danh sách yêu cầu không đồng nghĩa đã triển khai. Theo yêu cầu 04/10/2026, Match Look nằm trong cột công cụ Adjust, dùng chung preview và preset; engine Core vẫn riêng; không tự thu hẹp phạm vi hoặc tuyên bố tương đương thuật toán Capture One.
  Các công cụ đã nối ảnh thật và giới hạn thuật toán được ghi ở `docs/adjust-tools.md`. Layers/Masks chỉnh ảnh độc lập layers bố cục in. Không báo mask AI, RAW/ICC, camera live view thật hoặc export full resolution đã có khi hiện chưa tích hợp.
  Preset room dùng tên công cụ tiếng Anh và nhập XMP/CUBE: xem `docs/lightroom-presets.md`. Giữ draft riêng published; lưu một preset không phát hành draft khác. Đọc đúng số XMP không đồng nghĩa màu giống Adobe; CUBE cần kiểm chứng color space và ảnh đối chiếu. Hiện preset mặc định theo máy, chưa riêng mỗi dự án.
* **Retouch:** nghiên cứu nhận diện khuôn mặt/vùng da, làm đẹp da, mắt, răng và trải nghiệm theo Xingtu.
  Tab xử lý ảnh thật hiện có Windows face boxes, chỉnh riêng từng mặt, da/răng/mắt, căn tâm mắt/miệng thủ công và bút bảo vệ; xem `docs/retouch-tools.md`. Chưa có AI landmark/semantic segmentation, preset tự áp mọi ảnh camera hay backend thanh làm đẹp của khách. Retouch đã lưu gắn với ảnh mẫu và chạy trước bộ màu; live preview bỏ Retouch. Không tuyên bố tương đương Capture One.
* **Refine:** nghiên cứu các công cụ hoàn thiện ảnh trong nhóm tính năng người dùng đã gửi tham khảo.
  Tab đã xử lý RGB ảnh mẫu với Sharpening, Noise Reduction, Film Grain, Dust Removal thủ công, Purple Fringing và chroma Moiré; xem `docs/refine-tools.md`. Navigator/Focus chỉ xem ảnh, không lấy nét camera. Hiệu ứng trung tính khi mở ảnh mới; Refine đã lưu chạy sau màu, bỏ khỏi live view. Chưa có healing/AI dust, RAW/full-resolution export hoặc preset tự áp mọi ảnh camera; không tuyên bố tương đương Capture One.
* **AI Match Look:** nhận ảnh tham khảo và đề xuất công thức chỉnh màu có thể tinh chỉnh, lưu và dùng lại.
  Yêu cầu bổ sung 03/10/2026: mục tiêu giống màu trên 90% với quản trị tinh chỉnh, thư viện tone từ nhiều digital camera. Xem `docs/match-look-design.md`. Đây chưa phải kết quả đã đạt; phép đo/tolerance còn mở. Phân biệt ước lượng look với đọc thông số gốc, profile camera booth với tone camera tham khảo, chuẩn hóa từng ảnh với tone dùng lại. Chưa chọn AI model/AI engine.
  Paired Match v1 baseline đã triển khai 04/10/2026: `docs/paired-match.md`. Tối ưu Basic/Curves/HSL qua ColorProcessor, chuyển sang Adjust bản nháp và benchmark RGB RMSE có tập pixel check. Match Look trong Adjust nhận ảnh khác cảnh/tỷ lệ, ưu tiên Windows face regions ở v3 và fallback global v2: `docs/reference-match.md`, chọn reference trước rồi source, so quantile RGB/chroma với giới hạn, clipping/hue/saturation penalty; bỏ Match method/Paired khỏi GUI; không có held-out check/semantic matching. Không gọi là AI hoặc báo đã đạt 90%; Color Grading/LUT chưa fit. Shared image loader chuẩn hóa EXIF/profile và cạnh dài 960 px; dữ liệu camera thật chưa benchmark.
* **LUT riêng:** tạo và sử dụng file `.cube` của FotoAutomat.
  Nghiên cứu AI Match Look ngày 05/10/2026 ở `docs/ai-match-look/research-2026-10-05.md` và `training-plan.md` cùng thư mục: CanonCGT/PETAL/D-LUT là ứng viên thử nghiệm, chưa tích hợp/train hoặc có số đo AI. Người dùng dự kiến máy văn phòng nhỏ, chưa xác nhận cấu hình; benchmark CPU, tách train và tạo look khỏi áp preset ở booth, không mặc định GPU rời.
* **Overlay PNG:** tùy chọn.

Toàn bộ Adjust/Retouch/Refine vẫn thuộc phạm vi nghiên cứu được yêu cầu. Chưa có quyết định loại bỏ hàng loạt công cụ; việc chỉ đưa một phần vào MVP là đề xuất chưa được duyệt.

AI cần khảo sát các phương pháp mới và phù hợp, dùng chung bộ dữ liệu để so sánh: khớp thống kê màu làm mốc; khớp theo vùng nội dung và tìm thông số chỉnh màu; dự đoán công thức hoặc tạo LUT. **D-LUT** là ứng viên nghiên cứu; **Retouch4me Color Match** là công cụ đối chiếu trải nghiệm. Chưa chọn model/thuật toán cuối cùng, chưa benchmark trên ảnh booth của người dùng và chưa có bằng chứng một phương án đã thay thế được Capture One. Không trình bày ứng viên như quyết định đã chốt. Đánh giá cả chất lượng, tốc độ, lỗi, tài nguyên và giấy phép thương mại trước khi chọn hướng tích hợp.

### Phần cứng và vận hành đã xác nhận

Cập nhật 06/10/2026: người dùng chốt ASUS NUC 13 Pro Slim i7-1360P, RAM 16 GB, không yêu cầu GPU rời làm cấu hình mục tiêu. Các ghi chú cũ “chưa xác nhận cấu hình” bên trên không còn áp dụng cho lựa chọn này. Hiệu năng trên NUC thật vẫn chưa kiểm chứng; xem `docs/ai-match-look/cpu-deployment-target.md`.

| Hạng mục | Quyết định / giới hạn xác nhận |
| --- | --- |
| Hệ điều hành | Windows trước; chưa phát hành macOS. |
| Camera thử nghiệm | Fujifilm X-H2, Canon R50 và Canon R100. |
| Camera SDK | Nghiên cứu SDK Fujifilm/Canon cho điều khiển, live view và nhận ảnh; khả năng từng model phải được kiểm chứng, không mặc định tương thích. |
| Máy in mục tiêu | DNP DS620A; vẫn cần thử nghiệm tích hợp thực tế. |
| Giao diện booth | Màn hình cảm ứng. |
| Thanh toán | Tiền mặt và QR; chưa chốt model bộ nhận tiền hoặc nhà cung cấp thanh toán. |
| Offline | Chụp, xử lý ảnh cục bộ và in phải hoạt động khi mất Internet; tải lên/chia sẻ trực tuyến chờ kết nối. |
| GPU và hiệu năng | Cấu hình GPU và ngưỡng chất lượng/thời gian chờ còn phải xác nhận, không tự đặt thành cam kết. |

### Lộ trình và điều kiện chuyển phase

Các phase thể hiện hướng triển khai và điều kiện nghiệm thu, không phải báo cáo các phase đã hoàn thành. Tiến độ UI hiện tại không thay thế bằng chứng nghiệm thu này.

| Phase | Phạm vi thực hiện | Đầu ra và điều kiện chuyển phase |
| --- | --- | --- |
| **0. Chốt MVP và chuẩn bị thử nghiệm** | Hoàn thiện user stories; xác nhận camera, máy in, bộ nhận tiền, hệ điều hành, GPU, yêu cầu offline; chuẩn bị ảnh tham khảo và ảnh booth. | Backlog có ưu tiên, sơ đồ luồng, tiêu chí chất lượng và danh sách quyết định còn mở. |
| **1. Kiểm chứng AI Match Look** | Khảo sát phương pháp AI hiện hành; so sánh với khớp màu đơn giản trên cùng dữ liệu; kiểm tra xuất công thức và LUT. | Báo cáo ảnh trước–sau, tốc độ, lỗi, tài nguyên và giấy phép; bằng chứng chọn hướng đủ tốt để tích hợp. |
| **2. Xây công cụ quản trị màu** | Tải ảnh, chọn vùng tham khảo, nhận đề xuất, chỉnh cường độ/thông số màu, xem trên nhiều ảnh, lưu nháp và phát hành phiên bản. | Quản trị tự tạo, dùng lại bộ màu và quay về phiên bản trước được. |
| **3. Tích hợp camera và luồng chụp** | Kết nối, live view, áp màu, đếm ngược, chụp bộ ảnh, lưu ảnh gốc và xử lý mất kết nối. | Phiên chụp ổn định; xác minh độ khớp màu giữa preview và ảnh cuối. |
| **4. Tích hợp AI làm đẹp sau chụp** | Preview mức làm đẹp, áp trên ảnh gốc; kiểm tra ảnh nhiều người, kính, tóc và vùng bị che. | Giữ nhận diện, chi tiết da và màu đã chọn; đạt thời gian chờ đã thống nhất. |
| **5. Hoàn thiện giao dịch và đầu ra** | Thanh toán, chọn ảnh, ghép khung, hàng đợi in, tải ảnh bằng QR, trạng thái và phục hồi lỗi. | Chạy trọn phiên; thử lỗi thanh toán, mất mạng, máy in; tránh ghi nhận hoặc thực hiện trùng. |
| **6. Pilot và phát hành MVP** | Chạy booth thật; theo dõi chất lượng, thời gian phiên, lỗi, phản hồi khách; hướng dẫn vận hành. | Đạt tiêu chí phát hành; có quy trình hỗ trợ, cập nhật và khôi phục phiên bản. |

### Quyết định còn mở và nguyên tắc tiếp tục

Chưa chốt phạm vi MVP thu gọn, model/thuật toán AI, cấu hình GPU, ngưỡng chất lượng/độ trễ, bộ nhận tiền và nhà cung cấp thanh toán. Tương thích SDK từng camera và máy in cần bằng chứng thực tế. Giữ các mục này ở trạng thái chưa xác nhận cho đến khi có quyết định hoặc kết quả thử nghiệm.

Yêu cầu thương mại còn có kích hoạt thương hiệu bằng key. Xem `docs/activation-api.md` để biết đề xuất kiến trúc và trạng thái; không coi API điều khiển booth là hệ thống cấp phép, hoặc coi tài liệu đề xuất là dịch vụ đã triển khai. Khi làm tiếp, giữ yêu cầu sản phẩm này cùng các quyết định UI/UX hiện có; không yêu cầu người dùng chốt lại quyết định đã ghi nhận.

## Development rules

### UI copy — yêu cầu người dùng 19/10/2026

* Giữ màn hình cài đặt gọn: không đánh số tiêu đề nhóm; dùng nhãn ngắn, rõ công năng.
* Không đặt đoạn hướng dẫn phụ thường trực hoặc danh sách tính năng/backend chưa hoàn thiện trên UI (ví dụ chú thích flash, cách đổi mode/WB, danh sách chức năng đang chờ tích hợp).
* Không chuyển các lời giải thích đã bị yêu cầu xóa thành tooltip. Trang Máy ảnh không có tooltip hướng dẫn, chú thích vận hành thường trực hay danh sách backend chưa hoàn thiện. Hướng dẫn chi tiết chỉ nằm trong tài liệu hoặc nút Hướng dẫn do người dùng chủ động mở. Giữ trạng thái thiết bị và thông báo lỗi cần xử lý ngắn gọn, đúng sự thật.
* Giới hạn triển khai phải ghi trong tài liệu/báo cáo phát triển, không biến màn hình cài đặt thành báo cáo kỹ thuật.

* Keep Core independent of WPF, SQLite, and manufacturer SDKs.
* Put storage and device implementations in Infrastructure. Keep UI code in Desktop.
* Test shared behavior through the CLI harness and automated tests. Run the desktop UI on Windows before approving a Windows release.
* Default to simulated devices. Never accept real money, print on physical hardware, or trigger a camera without explicit authorization for that integration test.
* Persist intent before any external side effect. An uncertain print outcome requires reconciliation, never an automatic duplicate submission.
* Do not commit customer photos, credentials, device identities, vendor SDK binaries, runtime databases, or generated build output.
* Do not assume a connected camera is SDK compatible. Record evidence in docs/hardware-validation.md.
* Keep status claims accurate. A Windows cross build on macOS is not a Windows runtime test.
* Update relevant docs with behavior and command changes. Prefer small complete changes and meaningful failure tests.

## Verification

See docs/testing.md for the authoritative commands. Run the portable tests and simulator scenarios for shared logic changes. Run the Windows build for desktop changes. Do not report CI as passed until its run has actually completed.

## CodeGraph

If .codegraph exists, use codegraph explore or codegraph node before searching for or reading code to understand it. If the index is unavailable or stale, report that and use source tools. Do not reindex without the user's request.

## Agent skills

### Issue tracker

Work is tracked in Jira project SCRUM. See docs/agents/issue-tracker.md.

### Domain docs

Single context, with root CONTEXT.md and docs/adr/ when needed. See docs/agents/domain.md.

### Workflow

Use only the installed skills relevant to the task. Skill availability is not a requirement to load or invoke every skill. Explicit user instructions take precedence over skill defaults.
