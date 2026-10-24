# Adapter Fujifilm USB — bản thử nghiệm

## Đã triển khai

`src/FotoAutomat.Infrastructure/FujifilmCameraConnection.cs` triển khai `ICameraConnection` bằng XAPI x64. Không copy DLL hoặc mã sample của hãng vào repo. API được đối chiếu header và các phần liên quan trong `SDK_ProgrammingReference.pdf`, Rev. **1.34.1.0**: Init trang 75, OpenEx 89–90, Release 126, ReadImage 137, Start/StopLiveView 503–504. Đã trích xuất manual vào `.local/fuji-api.txt` để tra cứu nội bộ; không phát hành bản trích xuất.

- Một worker thread tuần tự và một chủ sở hữu SDK trong process.
- Nạp DLL từ đường dẫn cấu hình bằng LoadLibraryEx; truyền module handle cho Init để tìm thư viện phụ.
- Detect USB, yêu cầu đúng một camera, kiểm tra tether flag, đọc model thực.
- Lưu priority, chuyển sang PC priority cho live view, cố gắng khôi phục khi ngắt kết nối bình thường. Không đổi exposure, WB, media record hoặc film simulation.
- Live view qua SetProp Start/Stop và ReadImageInfo/ReadImage. Không gọi DeleteImage. ReadImage tự lấy ảnh ra khỏi buffer theo tài liệu hãng.
- Chụp qua half shutter rồi full shutter/release; AF failure thì không bấm full shutter. Poll JPEG tối đa 30 giây, không lặp lệnh shutter. RAW đi trước JPEG được lưu `.raf` cạnh JPEG; không hỗ trợ HEIF hay RAW-only cho luồng này.
- Ảnh cũ trong buffer làm dừng thao tác để tránh gán nhầm vào shot mới. Không tự xóa ảnh cũ.
- Nhật ký intent/receipt dùng lại từ BoothCameraPipeline; file gốc CreateNew, không ghi đè.

## Cách thử trên app

1. Đóng bản FotoAutomat cũ. Mở bản mới từ thư mục repo:

```powershell
& .\.local\builds\fuji-adapter\FotoAutomat.Desktop.exe --admin
```

2. Vào **Cấu hình chụp → Máy ảnh**, chọn **Fujifilm**.
3. Ô SDK chọn `C:\Users\Admin\Downloads\SDK13410`. Có thể chọn trực tiếp `SDK13410\REDISTRIBUTABLES\Windows\64bit` bên trong.
4. Kết nối đúng một máy Fujifilm bằng USB, đặt chế độ tether shooting trên camera; đóng phần mềm khác đang giữ máy. Dùng JPEG cho đợt thử đầu. Tên menu/chế độ phải đối chiếu đúng model/firmware.
5. Bấm **Kết nối USB**. Khi thành công, góc trên hiển thị model thật và live view bắt đầu.
6. Bấm **Chụp thử** để thực sự kích hoạt màn trập. Ảnh vừa chụp được giữ trên màn hình; bấm **Xem live view** để quay lại.
7. Bấm **Ngắt kết nối** trước khi rút cáp. **Lưu cấu hình** chỉ lưu model/đường dẫn/thiết lập hiển thị, không gửi exposure tới máy.

Ảnh thử và nhật ký nằm tại `%LOCALAPPDATA%/FotoAutomat/CameraTests/<session>/`. Preview quản trị chưa áp LUT; ảnh JPEG nhận được là ảnh gốc camera. Không nhận ảnh mẫu làm camera thật.

## Kiểm chứng đã thực hiện

- Windows build: 0 lỗi, 0 cảnh báo.
- Automated suite: 162/162; harness simulator: 4/4. Hai ca mới kiểm tra thiếu SDK và chọn đúng cây 64bit/XSDK.DAT; không kiểm chứng native API bằng fake hardware.
- Nạp XAPI.dll x64 thực bằng loader và kiểm tra đủ 13 export cần dùng: đạt. Không gọi Init, Detect, Open hoặc shutter trong phép kiểm này.
- Chưa chạy camera vật lý hoặc kiểm chứng WPF trực quan. Không tuyên bố X-H2 đã chụp thành công.

## Giới hạn còn lại

- ISO/khẩu độ/tốc độ/WB trên UI vẫn khóa; chưa nối đọc/ghi capability các thuộc tính này.
- Adapter đã nối trang quản trị. Startup booth mặc định vẫn mô phỏng, chưa chuyển tự động sang camera thật hoặc tích hợp ledger thanh toán. Nhánh MainWindow nhận pipeline có sẵn, nhưng chưa có composition root sản xuất.
- Timeout 30 giây chỉ chặn vòng poll; không cưỡng bức ngắt native call bị treo. Worker process để cách ly SDK treo là bước hardening tiếp theo.
- Khôi phục priority có thể thất bại khi camera bị rút USB/tắt nguồn. Sau lỗi, kiểm tra camera rồi ngắt/kết nối lại, không tự chụp bù.
- Nếu buffer có nhiều ảnh, RAW đến sau JPEG, hoặc burst ngoài dự kiến, pipeline có thể dừng thay vì tự bỏ ảnh. Kiểm thử và bổ sung chính sách chuyển file trước khi nghiệm thu booth.
- Chưa xác nhận màu live view/flash, ICC/EXIF đầy đủ hay hiệu năng NUC.

EULA/bảo hành đã ghi ở `fujifilm-sdk-intake-2026-10-19.md`; quyền dùng SDK không thay thế kiểm thử thiết bị.
