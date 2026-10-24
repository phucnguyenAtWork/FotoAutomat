# Nối giao diện Máy ảnh mới với Fujifilm SDK — 21/10/2026

## Đã triển khai

- `WorkspaceToolsPanel`: trang CameraPage mới dùng FujifilmPageCameraService + CameraPageConfigStore ở chế độ quản trị. `--ui-preview` và project CameraUiPreview vẫn mô phỏng.
- Mở thẳng bằng `FotoAutomat.Desktop.exe --camera-settings`; không tự kết nối hoặc chụp khi mở trang.
- SDK: ưu tiên đường dẫn đã lưu. Nếu trống, tìm FUJIFILM_SDK_DIR, Downloads/SDK13410, hoặc FujifilmSDK cạnh exe. Kiểm tra XAPI.dll và XSDK.DAT x64; đây không phải xác nhận đã nạp DLL hay tương thích thiết bị.
- Bộ chọn hiện mục tiêu `Fujifilm USB · tự nhận model`, KHÔNG phải danh sách thiết bị đã dò. Khi bấm Kết nối USB, adapter gọi XSDK_Detect/Open, yêu cầu đúng một camera, đọc model thật, mở live view và đọc capability.
- Canon/Nikon báo chưa có adapter, không thay bằng thiết bị giả.
- ISO, khẩu độ, shutter, AE, WB, Kelvin dùng options từ SDK. Chỉ gửi thông số thay đổi, kiểm tra lại capability trước khi áp, ghi nhật ký và đọc xác nhận. Mode/WB phải áp riêng theo giới hạn adapter.
- Chụp thử dùng BoothCameraPipeline: lưu intent trước màn trập, nhận JPEG gốc và receipt, không tự chụp lại khi lỗi. JPEG gốc lưu LocalAppData/FotoAutomat/CameraTests; ảnh trên UI được decode nhỏ để xem.
- Poll live view và các thao tác được tuần tự hóa. Lỗi frame ngừng kết nối, báo lỗi; rời trang dừng polling và đóng SDK. Lưu cấu hình chỉ lưu PC, không tự áp lên camera khi khởi động.
- CameraSetup lưu thêm ParameterValues; giữ mirror/rotation/preview width cũ. Trang camera mới chưa áp các tùy chọn hiển thị cũ này vào preview.

## Kiểm chứng và giới hạn

Build solution đạt 0 lỗi/0 cảnh báo. 184 tests đạt (bao gồm lưu/đọc cấu hình và tìm SDK trong thư mục giải nén lồng nhau); tests dùng dữ liệu giả, không nạp SDK hay vận hành máy thật. Chưa xác nhận kết nối, live view, ghi thông số hoặc chụp trên X-S20/X-H2. SDK chính thức có hỗ trợ X-S20 nhưng adapter vẫn cần thử phần cứng.

Bản build: `.local/builds/fuji-connected-2026-10-21/`; `Open-Camera.cmd` mở chế độ thật. Đóng bản cũ trước vì app chỉ cho một phiên chạy.

Đã chạy --camera-settings --render-preview: exit 0, ảnh camera-page.png được tạo và kiểm tra. Cấu hình PC hiện lưu Canon nên UI hiển thị đúng lỗi chưa có adapter Canon; người dùng chọn Fujifilm trước khi kết nối. Không tự đổi hãng đã lưu.
