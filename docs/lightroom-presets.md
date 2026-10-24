# Preset room trong FotoAutomat

GUI quản trị → **Bộ lọc màu → Adjust**. Công cụ chỉnh màu dùng tiếng Anh; Đổi ảnh, Xem ảnh gốc, Nhập LR và Lưu preset giữ tiếng Việt.

## Giao diện

Ảnh thử ở trái: lăn chuột zoom 1–8×, kéo di chuyển, Fit hoặc nhấp đúp đặt lại. Mouse/touch pan và mask dùng cùng tọa độ zoom. Xem ảnh gốc không ghi đè ảnh.

Cột phải 320 px. Histogram và chỉ số clipping cố định ngoài vùng cuộn. Highlight clipping đếm pixel có ít nhất một kênh bằng 255, Shadow clipping có ít nhất một kênh bằng 0; bỏ pixel trong suốt. Đây là clipping BGRA8, không phải đo khả năng cứu sáng RAW. Hàng 32 px: tên, slider, ô số. Enter/rời ô áp dụng; ngoài thang hiện viền đỏ.

Histogram cao 116 px gồm tiêu đề, đồ thị RGB/luminance và hàng EXIF ISO/Shutter/Aperture. Nền charcoal, lưới nhẹ, đường RGB mảnh với lớp màu mờ và luminance xám. Đường vẽ làm mượt bằng kernel 5 bin; clipping vẫn đo pixel gốc. Thông số EXIF đọc từ JPEG/TIFF nếu có, thiếu hiện dấu —, không lấy thông số mẫu để giả lập. Windows check riêng kiểm tra fixture EXIF ISO 125, 1/60 s, f/2.2, clipping có pixel trong suốt và render ở rộng 320/640 px.

| Group | Parameters / ranges |
| --- | --- |
| Basic | Temp −100…100 cho JPEG/TIFF; Temp (K) 2000…50000 khi XMP chứa Kelvin; Tint ±100 hoặc ±150 với Kelvin; Exposure ±5 EV; Contrast/Highlights/Shadows/Whites/Blacks/Texture/Clarity/Dehaze/Vibrance/Saturation ±100 |
| Tone Curve | RGB/Luma/Red/Green/Blue; điểm curve; Highlights/Lights/Darks/Shadows ±100 |
| Color Mix (HSL) | Red/Orange/Yellow/Green/Cyan/Blue/Purple/Magenta; Hue/Saturation/Luminance ±100. Orange bao gồm nhiều màu da, không phải mask AI |
| Color Grading | Global/Shadows/Midtones/Highlights; wheel Hue 0–360, Saturation 0–100, Luminance ±100; Blending 0–100, Balance ±100 |
| Effects | Vignette ±100; Grain 0–100 |
| Detail | Sharpening 0–150 |
| Calibration | Red/Green/Blue Primary Hue/Saturation ±100 |
| Levels / Layers / Masks | Giữ công cụ đã có; không có mask AI |
| Legacy adjustments | Brightness giữ cho công thức cũ, không giả danh thanh Lightroom |

Các thông số xử lý pixel trong Core. Tone Curve vùng dùng trọng số luminance; Vibrance tăng màu ít bão hòa; Calibration chỉnh trục opponent trên RGB; Vignette theo bán kính; Grain có seed ổn định; Sharpening dùng detail 3×3. Đây là thuật toán FotoAutomat, không phải engine Adobe, RAW camera calibration hay bằng chứng tương đương Lightroom.

## Nhập LR

Chọn nhiều `.xmp`/`.cube`, tối đa 32 MB/tệp. File nhập vào bản nháp; một file lỗi không hủy file khác.

* **XMP:** namespace Camera Raw, thuộc tính hoặc phần tử. Giữ số Basic PV2012, Texture/Clarity/Dehaze/Vibrance, curves, parametric curve, HSL, grading, Vignette/Grain/Sharpness và primary Calibration. XML cấm DTD/entity bên ngoài; số không hữu hạn, ngoài thang hoặc curve sai bị từ chối. CameraProfile, masks Adobe, optics, sharpening radius/detail/masking, thông số phụ Grain/Vignette, parametric split tùy chỉnh và trường chưa hỗ trợ được báo trong popup.
* **CUBE:** RGB 3D size 2–65, red changes fastest, DOMAIN_MIN/MAX, nội suy trilinear. Bảng được lưu cùng preset, không cần file nguồn còn tồn tại. CUBE 1D/bảng sai bị từ chối. LUT chạy trước các điều chỉnh bổ sung. CUBE không chứa con số Lightroom gốc: slider bắt đầu trung tính trên LUT.

Kelvin XMP giữ nguyên số nhưng chuyển RGB gần đúng quanh mốc 6500 K; chưa có RAW/profile Adobe. [Adobe phân biệt thang Temp RAW và non-RAW](https://helpx.adobe.com/ca/lightroom-classic/desktop/process-and-develop-photos/image-tone-color.html). Nhập đúng số không có nghĩa render giống Lightroom. CUBE còn phụ thuộc color space, domain, cách bake, interpolation và đầu vào; chưa có LUT HM và ảnh đối chiếu để xác nhận.

## Lưu và sử dụng

Lưu preset mở ô đặt tên. Giữ tên cập nhật preset đang chọn; tên khác tạo preset mới. Chỉ preset được lưu được phát hành, các bản nháp khác không bị kéo theo. Thư viện theo máy ở `%LOCALAPPDATA%/FotoAutomat/Operator/color-workspace.json`, tối đa 100 preset. Các bộ màu cũ giữ được và nhận ID ổn định khi lưu.

Default preset chọn từ thư viện đã lưu. Tông mặc định trong editor lưu preset đang chọn và chọn nó. Hiện mặc định theo booth/máy; chưa có mặc định riêng từng dự án/sự kiện. Khách thấy danh sách động preset đã lưu, chỉ chọn, không có slider.

Kéo thanh đổi bản nháp/ảnh thử quản trị. Booth đọc `Published`. Preview Capture bỏ Texture/Clarity/Dehaze/Vignette/Grain/Sharpening; ảnh cuối áp toàn bộ. Core có `ApplyLiveView` và `Apply`. Hiệu ứng đã bake trong CUBE không thể bóc riêng. Các nhóm khác vẫn tốn CPU, chưa có cam kết không tăng thời gian.

Booth vẫn dùng ảnh mẫu lặp lại. Camera SDK, ảnh chụp độc lập, xuất full resolution, in thực tế và dự án/sự kiện riêng chưa nối trong tác vụ này.

## Kiểm chứng

99 test portable, bốn scenario harness đạt. Debug/Release WPF build không warning/error. Windows runtime trên JPEG thật kiểm tra số nhập XMP/Enter, Levels/curve/HSL/grading/detail, layers/brush/eraser, undo/redo, preview pixel mới nhất, zoom, draft độc lập, lưu từng preset không lộ draft khác và thư viện hơn ba preset. Chưa benchmark với Lightroom hoặc kiểm thử cảm ứng/camera/máy in thật.
