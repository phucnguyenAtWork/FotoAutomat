# Kiểm tra UI import — 20/10/2026

6 file `2026-10-20-*` nằm ở gốc repo, chưa thuộc Desktop project. Đã thêm project chạy thử độc lập `tools/FotoAutomat.CameraUiPreview`, link trực tiếp các file import. Không thay trang camera SDK hiện tại.

## Sửa để chạy

- Root CameraPage dùng StaticResource PageBrush/TextBrush trước khi resource dictionary của trang được nạp: build qua nhưng runtime XamlParseException. Đổi hai tham chiếu root sang DynamicResource; cửa sổ đã mở.
- Bỏ tooltip và chú thích hướng dẫn thường trực theo yêu cầu UI đã xác nhận.
- Thêm host với ICameraService mô phỏng, ICameraConfigStore trong bộ nhớ, ảnh hình học để xem bố cục. Không tải SDK, không truy cập camera, không ghi cấu hình vận hành.

## Kết quả

Build Release: 0 lỗi, 0 cảnh báo. Đã mở cửa sổ thực bằng computer-use, kiểm tra trạng thái chưa kết nối, kết nối mô phỏng, thông số được nạp, ảnh mô phỏng và chuyển sang review khi bấm Chụp thử. Không coi đây là kiểm chứng camera thật, chưa kiểm tra toàn bộ thao tác/độ phân giải màn hình.

## Lưu ý trước khi tích hợp

- Layout import là preview lớn bên trái và hai card bên phải, khác yêu cầu ba cột đều trước đó. Chỉ giữ trong preview để đánh giá thiết kế mới.
- ICameraService hiện là hợp đồng; cần bridge với FujifilmCameraConnection/BoothCameraPipeline, giữ journal và readback.
- ApplyAsync đang MarkApplied toàn bộ lựa chọn sau lệnh Set, chưa đọc lại từ camera; cần sửa trước dùng SDK thật.
- Chưa có cleanup service khi rời trang; cần xử lý vòng đời trước tích hợp thật.
- Nút Mở ảnh hiện mở trình xem mặc định Windows, không nạp ảnh vào preview.
- Lưu trong bản thử chỉ giữ trong bộ nhớ, đóng app sẽ mất.

## Chạy

```powershell
dotnet run --project tools/FotoAutomat.CameraUiPreview -c Release
```
