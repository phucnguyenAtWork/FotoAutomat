# Phạm vi Adjust từ ảnh tham khảo — 02/10/2026

Trạng thái đã cập nhật sau triển khai; xem [công cụ đang hoạt động và giới hạn](adjust-tools.md).

Người dùng đề xuất áp dụng các nhóm công cụ thể hiện trong chín ảnh Capture One. Đây là phạm vi yêu cầu để triển khai tiếp, không phải báo cáo các tính năng đã hoàn thành. Tham khảo chức năng; FotoAutomat giữ thiết kế riêng phù hợp Windows và cảm ứng. Không suy ra thuật toán nội bộ hoặc chất lượng tương đương Capture One từ ảnh giao diện.

| Nhóm | Công cụ thấy trong ảnh | Trạng thái FotoAutomat |
| --- | --- | --- |
| Histogram | Phân bố RGB, đọc mức sáng/màu | Đã có histogram RGB của ảnh preview |
| White Balance | Kelvin, Tint, chế độ cân bằng trắng, hút màu | Có gain ấm/lạnh cơ bản; chưa có Kelvin/Tint hoặc hút màu |
| Exposure | Exposure, Contrast, Brightness, Saturation | Đã có Exposure/Contrast/Brightness/Saturation trên ảnh thật |
| High Dynamic Range | Highlights, Shadows, Whites, Blacks | Đã có Highlights/Shadows/Whites/Blacks trên ảnh thật |
| Levels | Input/output black/white, midtone, kênh RGB/Red/Green/Blue | Đã có bốn kênh, input/output và gamma |
| Curve | RGB, Luma, Red, Green, Blue; thêm/kéo điểm | Đã có năm kênh, thêm/kéo/xóa điểm |
| Color Editor | Basic/Advanced/Skin Tone; Hue/Saturation/Lightness theo dải màu | Đã có HSL tám nhóm màu; Advanced/Skin Tone riêng chưa có |
| Color Balance | Master và ba vùng Shadows/Midtones/Highlights | Đã có Master và ba vùng sáng |
| Black & White | Bật/tắt; độ nhạy Red/Yellow/Green/Cyan/Blue/Magenta; Split Tones | Có chuyển trắng đen toàn ảnh; chưa có mix màu/split tones |
| Clarity / Structure | Tương phản cục bộ và chi tiết | Đã xử lý tương phản cục bộ và chi tiết |
| Dehaze | Amount và Shadow Tone | Đã có xử lý lớp mờ cục bộ, xem giới hạn thuật toán |
| Vignetting | Amount và cách áp vùng tối/sáng viền | Chưa triển khai |
| Layers & Masks | Lớp, opacity, brush/eraser, kết hợp/tinh chỉnh mask; Subject/Background/People | Đã có lớp, opacity, bút vẽ/tẩy, radial/gradient, đảo mask; mask AI chưa có |
| Style Brushes | Brush mang thông số màu/sáng/tương phản; custom styles | Chưa triển khai |
| Match Look | Nhập một/nhiều ảnh tham khảo và áp đề xuất | Giữ module riêng AI Match Look; chưa chọn model hoặc benchmark |

## Thiết kế và hành vi cần giữ

* Preview lớn ở vùng trung tâm, nhóm công cụ trong thanh bên có thể thu gọn; histogram gọn phía trên; các bộ màu bên dưới. Không dùng bố cục tham khảo làm chuẩn pixel để sao chép.
* Các nhóm có bật/tắt và đặt lại riêng. Thanh kéo cảm ứng đi cùng ô số; nhập số và Enter phải cập nhật ảnh.
* Một ảnh gốc dùng cho mọi bộ màu. Mỗi bộ giữ công thức riêng; preview, thumbnail, nguồn live view và ảnh xuất dùng chung bộ xử lý được hỗ trợ.
* Giữ ảnh gốc, lưu công thức; undo/redo; kết quả preview cũ không được ghi đè yêu cầu mới.
* Layers/masks chỉnh ảnh độc lập layers bố cục; mask có vùng và opacity riêng, không phá ảnh gốc.
* Không thêm nút hoạt động giả. Phân biệt công cụ đã xử lý pixel với nhóm đang nghiên cứu/chưa tích hợp.

## Thứ tự triển khai đề xuất

1. Histogram, White Balance/Tint, Brightness, HDR: mở rộng trực tiếp pipeline ảnh thật hiện có.
2. Levels và Curve: nền điều khiển tương phản và màu theo kênh.
3. Color Editor/HSL, Color Balance, Black & White mix và Split Tones.
4. Clarity, Structure, Dehaze, Vignetting: kiểm tra chi tiết, halo, màu da và hiệu năng.
5. Layers/Masks và Style Brushes; mask tự động cần nghiên cứu/kiểm chứng riêng.
6. Match Look tạo công thức có thể sửa tiếp hoặc LUT, đánh giá trên ảnh booth.

Thứ tự là đề xuất kỹ thuật, không loại bỏ các nhóm phía sau khỏi phạm vi. Trước khi nghiệm thu mỗi nhóm cần chứng minh thay đổi pixel đúng vùng, neutral giữ ảnh, lưu/tải công thức và preview thống nhất với ảnh xuất. Hiện camera live view thật và xuất full resolution chưa nối.
