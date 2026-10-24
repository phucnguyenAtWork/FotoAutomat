# Bộ chỉnh màu với ảnh thật

> Cập nhật: [Preset room và nhập Lightroom](lightroom-presets.md) bổ sung các nhóm, nhập XMP/CUBE, zoom/pan, thư viện nhiều preset và draft/published. Tài liệu dưới giữ mô tả giai đoạn đầu.

> Levels, Curves, HSL, Color Balance, Clarity/Structure/Dehaze và Layers/Masks hiện đã nối xử lý pixel. Xem [công cụ Adjust và giới hạn](adjust-tools.md) để biết thao tác và trạng thái mới nhất.

## Bổ sung Histogram và tone controls

Adjust có histogram RGB của preview hiện tại, Tint xanh/hồng, Brightness và Highlights/Shadows/Whites/Blacks. Tất cả tác động lên pixel, lưu cùng công thức và dùng trên preview booth. Thanh kéo đi cùng ô số; Enter hoặc rời ô áp dụng giá trị trong giới hạn, giá trị sai có viền đỏ. Histogram chuyển sang ảnh gốc khi bật Trước/Sau. HDR ở đây là điều chỉnh theo vùng độ sáng của một ảnh, không ghép bracket và không khôi phục chi tiết đã cháy trong JPEG. Warmth/Tint vẫn là gain màu cơ bản, chưa phải Kelvin có profile camera. Các công cụ còn lại trong adjust-scope.md vẫn cần triển khai.

Phạm vi mở rộng từ chín ảnh tham khảo Capture One được ghi tại [Adjust scope](adjust-scope.md), gồm trạng thái từng nhóm và thứ tự triển khai đề xuất.

Mở GUI quản trị → **Bộ lọc màu → Adjust → Chọn ảnh mẫu**. Hỗ trợ JPEG, PNG, BMP và TIFF qua decoder Windows.

Ba thẻ dùng cùng ảnh gốc, mỗi thẻ có công thức riêng. Chọn thẻ rồi kéo phơi sáng, tương phản, nhiệt độ màu, độ bão hòa hoặc bật trắng đen. Preview và các thumbnail cập nhật; nút Trước / Sau đổi giữa ảnh gốc và bộ màu hiện tại. Đặt lại chỉ reset bộ đang chọn.

**Lưu bộ màu** lưu ba công thức và đường dẫn ảnh mẫu vào cấu hình cục bộ. Ảnh gốc không bị sửa hoặc chép vào repo. Để tải lại ảnh mẫu sau khi mở ứng dụng, giữ tệp ở đường dẫn đã chọn. Thay đổi chưa lưu vẫn được chia sẻ giữa các cửa sổ trong lần chạy hiện tại.

Mở **Khởi động giao diện booth**: màn hình chọn màu dùng cùng ảnh mẫu với ba màu khác nhau. Bộ đã chọn hiển thị ở màn hình chụp mô phỏng, chọn ảnh, làm đẹp, ô khung đã gán và gallery cuối phiên. Các shots hiện dùng lại ảnh mẫu, chưa phải các ảnh camera riêng biệt.

## Giới hạn hiện tại

* Đây là xử lý màu ảnh thật trên nguồn ảnh mẫu, không phải camera live view. Chưa tích hợp SDK Canon/Fujifilm.
* Exposure xử lý trong linear light; nhiệt độ màu là điều chỉnh gain đỏ/xanh cơ bản, chưa phải Kelvin/tint có profile camera.
* Preview chuẩn hóa EXIF/profile màu, cạnh dài tối đa 960 px; giới hạn ảnh nguồn 40 triệu pixel và tệp 40 MB. WIC đọc ảnh nguồn trước khi kiểm tra kích thước và thu nhỏ. Thay đổi được gộp 90 ms, render ngoài luồng UI, kết quả cũ không thay thế kết quả mới.
* Không ghi đè ảnh gốc. Chưa xuất ảnh độ phân giải đầy đủ hoặc nối công thức vào hàng đợi in thật.
* Retouch/Refine vẫn là bản nháp thông số; AI Match Look chưa tích hợp. Không coi slider làm đẹp là xử lý AI đã hoạt động.
* Hiện có ba bộ màu trong workspace, chưa có version/publish/rollback hoặc thư viện bộ màu không giới hạn.

## Kiểm chứng

86 test đạt; build WPF không warning/error; bốn kịch bản harness đạt. Windows runtime đã tải ảnh JPEG thật, render ba màu và các màn hình khách. Core tests kiểm tra neutral giữ pixel, exposure/warmth/monochrome đổi pixel, giữ alpha, không sửa nguồn và không ghi đè cấu hình tốt khi lưu lỗi.
