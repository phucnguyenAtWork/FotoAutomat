# Camera settings — cập nhật 19/10/2026

Trang Máy ảnh dùng layout riêng FotoAutomat: kết nối gọn phía trên, preview lớn ở giữa, thông số trong cột phải có cuộn. Nút Hướng dẫn giải thích thao tác. Chưa kết nối hiển thị dấu — thay vì thông số giả.

## Đã nối

- Fujifilm SDK: đọc capability và giá trị thực của mode P/A/S/M, tốc độ, khẩu độ, ISO, WB, Kelvin. Chỉ liệt kê các giá trị camera trả về. Lỗi từng thông số được hiển thị riêng; không giả định mọi model hỗ trợ.
- Áp lên máy: kiểm tra capability lại, ghi intent xuống đĩa trước lệnh Set, đọc lại từng thông số để xác nhận. Không tự gửi lại nếu lỗi. Một nhóm lệnh có thể áp một phần, UI báo rõ và đọc lại.
- Áp riêng mode hoặc WB trước các giá trị phụ thuộc. Kelvin chỉ áp khi WB đang ở Color Temperature. Bulb không thuộc luồng booth hiện tại.
- Live view, chụp JPEG thử, trở lại live view; tùy chọn mirror/rotation chỉ ảnh hưởng preview, không đổi ảnh vừa chụp.
- Lưu cấu hình PC lưu hãng/model mục tiêu, đường dẫn SDK và hiển thị; không lưu/gửi lại exposure tự động.

## Cách thử

Mở `.local/builds/camera-controls/FotoAutomat.Desktop.exe --admin`, chọn Cấu hình chụp → Máy ảnh. Chọn Fujifilm và thư mục SDK; cắm một camera USB tether rồi Kết nối USB. Đọc từ máy; chọn mode hoặc WB rồi Áp lên máy riêng. Sau đó chỉnh ISO/tốc độ/khẩu độ/Kelvin và Áp lên máy. Chụp thử chỉ xảy ra khi người vận hành bấm nút.

## Phạm vi chưa hoàn thiện so với ảnh mẫu

Canon/Nikon native SDK, nhiều camera, nickname từng thiết bị, camera phụ full-res, quay video phiên, in test và save/recover event chưa được nối trong trang này. Chất lượng JPEG/tỷ lệ ảnh vẫn chỉnh trên camera. Không thêm checkbox giả cho các pipeline này. Tốc độ ít dùng chưa có tên trong bảng hiển thị sẽ ghi mã SDK, không đoán thời gian.

## Vị trí code

- `src/FotoAutomat.Desktop/CameraSettingsPanel.xaml`: layout.
- `src/FotoAutomat.Desktop/CameraSettingsPanel.xaml.cs`: tạo các ô từ capability, read/apply/readback, hướng dẫn, trạng thái.
- `src/FotoAutomat.Infrastructure/FujifilmCameraSettings.cs`: các hàm Cap/Get/Set Cdecl, Windows long 32-bit, worker tuần tự dùng chung adapter; journal intent.
- `tests/FotoAutomat.Tests/FujifilmSettingsTests.cs`: chặn thay đổi phụ thuộc và khóa key hợp lệ; kiểm tra đơn vị hiển thị.

## Kiểm chứng

Build Release thành công, 0 cảnh báo/lỗi; 166/166 test đạt, harness 4/4 đạt. Đây là kiểm chứng build/logic, không phải bằng chứng SDK tương thích camera thật. Chưa chạy kết nối hoặc màn trập thực. Đã mở bản quản trị mới qua computer-use. Kích hoạt cửa sổ thành công sau một lần thử lại; điều hướng trang camera bị gián đoạn bởi input của người dùng. Chưa xác minh trực quan bố cục trang camera.


Cập nhật UI theo phản hồi: bỏ toàn bộ tooltip trong trang camera và các chú thích hướng dẫn thường trực. Không chuyển chú thích bị xóa sang tooltip. Bản Release tiêu chuẩn đã build lại thành công; các build thử trong .local trước đó là bản cũ.
