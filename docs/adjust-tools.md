# Adjust: các nhóm đã nối ảnh thật

**Cập nhật 02/10/2026:** công cụ màu dùng tiếng Anh, thêm Basic/Tone Curve/Color Mix/Color Grading/Effects/Detail/Calibration, nhập XMP/CUBE, thư viện preset và tách draft/published. Histogram cố định, ảnh zoom/pan. Xem [Preset room](lightroom-presets.md) cho trạng thái hiện tại; các mục dưới mô tả phiên bản trước cập nhật.

## Kiểm tra trong ứng dụng

GUI quản trị → Bộ lọc màu → Adjust → Chọn ảnh mẫu. Chọn một trong ba bộ màu; mở nhóm công cụ ở thanh bên phải. Cân bằng trắng, Phơi sáng và Vùng sáng/tối là các nhóm riêng có thể thu gọn.

Giao diện Adjust dùng thanh bên 320 px, hàng thông số 32 px: tên, thanh kéo mỏng và ô số trên cùng một hàng. Histogram cao 80 px; preview cao 420 px; bộ màu mẫu bên dưới. Style compact chỉ áp dụng bộ chỉnh màu, không làm nhỏ thanh kéo trên giao diện khách.

| Nhóm | Thao tác đã hoạt động |
| --- | --- |
| Levels | Kênh RGB/R/G/B; đen/trắng đầu vào, gamma, đen/trắng đầu ra; đặt lại từng kênh |
| Curves | Kênh RGB/Luma/R/G/B; chạm để thêm điểm, kéo điểm, xóa điểm đang chọn; đặt lại kênh |
| HSL | Tám nhóm màu; Hue/Saturation/Lightness với vùng chuyển tiếp; đặt lại nhóm |
| Color Balance | Master/Shadows/Midtones/Highlights; hue, cường độ và độ sáng từng vùng |
| Clarity/Structure | Tăng hoặc giảm tương phản cục bộ/chi tiết theo hai bán kính |
| Dehaze | Giảm lớp mờ hoặc thêm hiệu ứng mờ bằng giá trị âm |
| Layers | Thêm/xóa, đổi tên, bật/tắt, opacity, đổi thứ tự; chọn Toàn ảnh hoặc một lớp để chỉnh |
| Masks | Toàn ảnh, bút vẽ/tẩy, vùng tròn, gradient; tâm/bán kính/góc, mềm biên, đảo mask và overlay đỏ |

Lớp mới mặc định áp toàn ảnh. Để chỉnh một vùng, chọn Bút vẽ, bật Vẽ mask trên ảnh rồi vẽ bằng chuột hoặc cảm ứng. Bút tẩy xóa bớt mask; nút Xóa mask xóa toàn bộ nét. Các thông số bên dưới chỉnh lớp đang chọn. Mask có tọa độ chuẩn hóa theo ảnh, không theo phần nền trống quanh preview.

Ô số nhận Enter hoặc rời ô. Levels tự giới hạn điểm đen/trắng để tránh đảo hoặc chia cho 0 và hiển thị lại giá trị thực đã áp dụng. Curve giữ hai đầu mút, không cho điểm vượt qua nhau và giới hạn 32 điểm/kênh.

Lưu bộ màu lưu tất cả nhóm, các lớp và mask. Hoàn tác/Làm lại áp cho chỉnh sửa trong lần chạy hiện tại, tối đa 100 mốc; đổi ảnh mẫu xóa lịch sử. Mỗi bộ màu giữ công thức riêng. Ảnh gốc không bị ghi đè. Preview của booth dùng cùng pipeline với preview quản trị.

## Thuật toán và giới hạn

Đây là các thuật toán của FotoAutomat, không phải engine hoặc công thức nội bộ Capture One. Curves nội suy tuyến tính qua các điểm; HSL chỉnh tám vùng hue có trọng số chuyển tiếp; Color Balance trộn màu theo trọng số luminance. Clarity/Structure dùng trung bình vùng và detail; Dehaze dùng ước lượng lớp mờ cục bộ, chưa có thuật toán khôi phục cảnh hay model AI. Chưa có kết luận chất lượng tương đương Capture One.

Lớp được áp tuần tự lên kết quả lớp trước, dùng opacity nhân mask. Hiện hỗ trợ tối đa 16 lớp, 10.000 dab/mask, không có lớp lồng nhau. Không có mask AI Subject/Background/People, chọn màu tự động hoặc Style Brushes. Undo của nét bút đang theo đoạn thao tác, chưa gộp cả một nét dài thành một bước.

Đang dùng pipeline BGRA 8-bit trên preview giới hạn 960 px, chưa có xử lý RAW/ICC/profile camera, export full resolution hay camera SDK thật. Warmth/Tint chưa thay thế Kelvin có profile. Các nhóm B&W mix/Split Tones, Vignetting, Skin Tone nâng cao, AI Match Look và LUT vẫn nằm trong phạm vi mở rộng riêng.

## Kiểm chứng

94 test đạt, build WPF Release không warning/error, bốn kịch bản harness đạt. Windows runtime test trên JPEG thật kiểm tra Levels, nhập gamma và Enter, thêm điểm Curve, HSL, Color Balance, Clarity/Dehaze, lớp, vẽ/tẩy mask, undo/redo và kết quả preview mới nhất. Đã rà ảnh render. Chưa kiểm thử trên màn hình cảm ứng hoặc camera/máy in vật lý.

## Cập nhật UI 17/10/2026

**Ngôn ngữ và kích thước**
- Nhãn, nút và thông báo dùng tiếng Việt.
- Tên thông số và công cụ giữ tiếng Anh, theo cách đặt tên của Capture One.
- Nút, ô chọn và checkbox cao 44 px (vùng chạm). Dòng slider cao 40 px. Kích thước dùng chung nằm trong `EditorUi`.

**Bố cục**
- Editor lấp đầy trang, không còn cao cố định 650 px.
- Cột công cụ rộng 340–480 px; phần còn lại dành cho ảnh xem trước.

**Danh sách preset**
- Hiển thị thành một hàng ngang, mỗi thẻ rộng 136 px.
- Trên màn cảm ứng vuốt ngang để cuộn; con lăn chuột cũng cuộn ngang.

**Phím tắt và slider**
- Ctrl+Z hoàn tác; Ctrl+Y hoặc Ctrl+Shift+Z làm lại. Khi con trỏ đang ở ô nhập chữ, ô đó giữ undo riêng của nó.
- Nhấp đúp slider để đặt về 0.

**Undo:** một lần kéo liên tục slider hoặc bánh xe trên cùng preset được tính là một bước hoàn tác. Các thay đổi cách nhau dưới 600 ms được gộp lại.

**Color Editor** (thay nhóm Color Mix):
- 8 ô màu Red … Magenta;
- Hue, Saturation, Lightness, mỗi thanh có dải màu nền;
- vẫn dùng `ColorRecipe.Hsl`.

**Color Balance** (thay nhóm Color Grading):
- các tab Master · 3-Way · Shadow · Midtone · Highlight;
- 3-Way: Midtone ở trên, Shadow và Highlight ở dưới;
- mỗi bánh xe có cung Lightness bên phải (kéo lên là sáng hơn);
- nhấp đúp vào bánh xe để đặt lại;
- tab đơn có thêm Hue, Saturation, Lightness;
- Blending và Balance nằm ở tab 3-Way;
- vẫn dùng `ColorRecipe.Balance[4]`.

**Hiệu năng:** preview lưu sẵn ảnh đã render cho từng preset. Đổi một thông số chỉ render lại preset đó, thay vì render lại toàn bộ preset và bản live view.

**Kiểm tra:**
- Build Release không có warning; 148 test đạt.
- Render offscreen ở 1100×780 và 1600×1000.
- Chưa thử trên màn cảm ứng thật.

**Levels** (theo cách bố trí của Capture One, 17/10/2026):
- histogram của ảnh nguồn, kèm tab RGB / Red / Green / Blue;
- nút dưới: điểm đen đầu vào, midtone, điểm trắng đầu vào;
- nút trên: điểm đen và điểm trắng đầu ra;
- mọi nút kéo được, mọi giá trị nhập được bằng số;
- nhấp đúp để đặt lại kênh.

Midtone nằm tại mức đầu vào cho ra 50% đầu ra: t = 0,5^Gamma. Ô giữa hiển thị **Gamma**: 1,00 là không đổi, lớn hơn 1 là sáng hơn. Ô này **không** dùng thang hiển thị 0,00 của Capture One.

Chưa có công cụ hút điểm đen/trắng (eyedropper).

## Cập nhật 21/10/2026

Các nhóm Adjust và Color Editor mới đã được tách theo yêu cầu. Xem [chi tiết triển khai, kiểm chứng và giới hạn](adjust-tools-2026-10-21.md); phần lịch sử bên trên không phản ánh đầy đủ phiên bản này.
