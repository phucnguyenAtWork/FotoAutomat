# Pipeline camera Canon / Fujifilm / Nikon

## Trạng thái thực tế

Đã triển khai hợp đồng driver chung, bộ điều phối nhận ảnh và nhánh WPF nhận pipeline qua constructor. **Chưa có adapter SDK native cho bất kỳ hãng nào; khởi động app mặc định vẫn là mô phỏng.** Không có menu bật camera thật, không tự tải DLL, không tự mở thiết bị. Không coi việc build thành công là đã hỗ trợ tất cả model của ba hãng.

## Những phần đã nối

`ICameraDriver.DiscoverAsync → Open → BoothCameraPipeline.ConnectAsync → StartLiveView → MainWindow → Countdown → CaptureAsync → JPEG original → CompleteCameraCapture → Review → Selection`.

- Core `CameraConnection.cs`: nhận diện hãng/model và capability live view/chụp; hợp đồng kết nối, lấy frame JPEG, dừng live view, chụp và đóng thiết bị. Adapter native phải điều phối SDK đúng thread của hãng, xử lý callback, disconnect và cancellation; không tự chụp lại khi timeout.
- Infrastructure `CameraDriverRegistry.cs`: đăng ký driver theo hãng; thiếu driver báo lỗi, không fallback simulator.
- Infrastructure `BoothCameraPipeline.cs`: serialize thao tác bằng semaphore, bỏ lượt preview khi đang tải ảnh. Ghi và flush `shot.intent.json` trước thao tác camera; kiểm marker đầu/cuối JPEG và ghi `shot.received.json` sau nhận file. Không ghi đè ảnh gốc; shot đã có intent bị từ chối khi mở lại. Marker JPEG không thay thế kiểm tra decode; WPF còn decode trước xác nhận đếm ảnh.
- Core `TimedCapture`/`CustomerPreviewFlow`: chế độ acknowledgement không tự tăng số ảnh theo timer; bắt đầu review sau nhận ảnh, giữ đường dẫn riêng từng ảnh; lỗi dừng lượt chụp.
- Desktop `MainWindow(BoothCameraPipeline)`: kết nối trước bắt đầu đếm ngược; poll preview không chồng tác vụ; render màu trên worker, giữ recipe đã chọn tại thời điểm bắt đầu chụp; preview live rộng 640, review 1280. Dùng `ColorProcessor.ForLiveView` cho live và recipe đầy đủ cho review. Lưu JPEG gốc không áp màu. Chọn ảnh/ghép ô/delivery dùng ảnh riêng theo số thứ tự ở nhánh camera.
- Đóng cửa sổ yêu cầu hủy và dispose connection. SDK adapter phải hoàn tất hủy/đóng; native SDK bị treo cần giải pháp worker process trong bước triển khai adapter.

## SDK cần chuẩn bị

| Hãng | Nguồn chính thức | Trạng thái adapter |
| --- | --- | --- |
| Canon | [Developer Resources châu Á](https://asia.canon/en/campaign/developerresources), EDSDK cho hướng USB | Chưa triển khai; cần gói SDK, header, sample, điều khoản và xác minh R50/R100 theo phiên bản |
| Fujifilm | [Camera Control SDK](https://www.fujifilm-x.com/en-ph/camera-control-sdk/) | Chưa triển khai; trang hãng liệt kê X-H2; cần kiểm thử live view/chụp đúng firmware |
| Nikon | [SDK chính thức](https://sdk.nikonimaging.com/information/en/) | Chưa triển khai; chưa chọn model Nikon, cần xác minh SDK/module tương ứng |

Đã đọc nguồn ngày 19/10/2026. Fujifilm có luồng tải riêng cá nhân/doanh nghiệp, EULA và lưu ý bảo hành cho cá nhân trên trang SDK; cần đọc đúng điều khoản trước dùng/phân phối. Không đưa SDK vendor vào Git. Không suy ra mọi model Nikon/Canon/Fuji đều tương thích.

## Những phần chưa hoàn thiện

1. Native driver cho ba hãng, discovery thực, bảng model/firmware đã thử, setup quản trị và composition root đăng ký driver vào app. Constructor injection hiện chỉ là điểm nối đã biên dịch, chưa được startup sử dụng.
2. Chưa kiểm chứng WPF runtime và tốc độ live view trên NUC; chu kỳ poll 100 ms không phải cam kết 10 fps. Chưa đo timeout của SDK, reconnect thực hoặc rút USB.
3. Intent/receipt là nhật ký file, chưa có UI reconciliation hay liên kết ledger thanh toán. Không tự tiếp tục phiên bị gián đoạn bằng shot ID mới.
4. Chưa xử lý full-resolution ảnh cuối, EXIF orientation/ICC đầy đủ ở nhánh camera, Retouch/Refine tự động, in hoặc upload. Preview màu và file JPEG gốc là hai sản phẩm khác nhau.
5. Chưa có profile riêng live view/flash. Cùng recipe không bảo đảm màu preview giống JPEG có flash. Tiếp tục giữ quyết định camera M/WB/flash cố định và đèn liên tục; đo bảng màu dưới điều kiện thực.
6. Luồng khách còn payment/printing mô phỏng. Nhánh camera này chưa sẵn sàng dùng bán hàng.

## Kiểm tra

Build Windows Release vào `.local/builds/camera-pipeline/`: 0 cảnh báo, 0 lỗi. Automated suite: 159/159 đạt, gồm 9 ca mới về receipt/countdown, driver thiếu, intent trước shutter, JPEG tải dở và chặn chụp trùng sau mở lại. Harness cũ: 4/4 đạt. Tests dùng fake camera, không kết nối/chụp máy thật.

```powershell
dotnet build FotoAutomat.slnx --configuration Release --no-restore -p:OutDir=C:/Users/Admin/Desktop/Work/FotoAutomat/.local/builds/camera-pipeline/
dotnet vstest .local/builds/camera-pipeline/FotoAutomat.Tests.dll
dotnet .local/builds/camera-pipeline/FotoAutomat.Harness.dll
```

## Tiếp theo

Chuẩn bị SDK chính thức cho mỗi hãng. Triển khai một adapter hoàn chỉnh trước, chạy discovery không chụp, sau đó kiểm thử được cho phép: live view, chụp đủ bộ, đóng app khi tải ảnh, rút USB, callback trễ/trùng và reconnect. Ghi model, firmware, SDK, driver Windows và kết quả vào `hardware-validation.md`. Sau khi driver đầu tiên đạt, chạy cùng bộ kiểm tra hợp đồng cho hai hãng còn lại.
