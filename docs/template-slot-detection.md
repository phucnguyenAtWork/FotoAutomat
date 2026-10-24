# Tạo khung từ ảnh

## Quyết định mới: chỉ quét #C6C6C6

Luồng desktop hiện gọi `DetectStandardGray`, thay thế nhận diện tự động nhiều nhánh bên dưới. Chỉ lấy pixel gần RGB (198,198,198), sai số mỗi kênh ±2, alpha ≥250. Gom liên thông 4 hướng, không nối qua khe, bỏ vùng chạm biên, bỏ vùng nhỏ hơn 0,5% ảnh và vùng có độ kín dưới 90%. Không dùng vùng trong suốt, magenta, trắng hay trời–cỏ làm ô. Overlay chỉ khoét màu đích trong các ô tìm được, không mở rộng để khử trắng. Chuẩn thiết kế đề nghị: PNG có các ô đặc #C6C6C6 tách rời nhau.

Nút quét và màn chờ có biểu tượng vector `smart-scan.svg`, được biểu diễn bằng DrawingImage trong WPF. Màn chờ hiện “Đang quét ô ảnh…” với progress bar vô định trong khi tác vụ chạy nền. Lời nhắc bắt đầu chỉ hiện khi không có ảnh khung và không có lớp. Trong cửa sổ hiện tại, quét lại kết quả vừa áp dụng dùng ảnh nguồn trước khi khoét; sau khi mở lại mẫu đã khoét, cần nhập lại thiết kế nguồn nếu muốn quét màu lần nữa.

Đã sửa nút Quét để không gọi kiểm tra của thao tác Lưu (kiểm tra đó yêu cầu có ít nhất một ô ảnh). Kiểm tra WPF thực tế bằng nút Quét, cửa sổ xem trước và Áp dụng: từ khung mới có 0 ô tạo 4 ô; từ khung có 1 ô thêm thủ công cũng thay bằng đúng 4 ô. Ảnh thử dùng bốn vùng #C6C6C6 riêng biệt.

Các mô tả nhiều nhánh sau đây ghi lại thuật toán web đã chuyển và vẫn được giữ trong Core để đối chiếu; không phải chế độ quét desktop đang sử dụng. Kiểm thử mới: bốn ô xám trên nền trong suốt có khe hẹp vẫn là bốn ô; màu khác và nền xám chạm biên không được nhận. 72 unit tests đạt, build WPF thành công.

Nguồn chuyển đổi: repo Photobooth cục bộ, nhánh `main`, commit `34ef975ef224e387e791a9bcee263fe8968aef11`. `origin/main` cục bộ trùng commit này; chưa xác minh trạng thái remote mới hơn.

Mã tham chiếu: `Apps/Api/Source/Modules/Templates/Service.ts` (detectSlotsInBuffer, extractSlotAperture và xử lý placeholder), `Packages/Shared/Source/WhiteMatte.ts` (khử white matte trong vùng chọn).

## Sử dụng

Cập nhật thao tác: nút **Quét ô ảnh** nằm ngay cạnh **Nhập khung ảnh**. Nếu đã có khung, quét ảnh đang dùng, xem trước rồi **Áp dụng ô ảnh** để thay các ô ảnh cũ; giữ chữ/logo/QR, tên mẫu, DPI và màu nền. Có thể Hoàn tác; không tạo preset mới hoặc tự lưu. Hủy hoặc không tìm thấy ô không thay đổi bố cục. Nếu chưa nhập ảnh, nút Quét mở bộ chọn tệp và tạo bố cục từ ảnh như luồng bên dưới. Hút màu/bút sửa thủ công chưa có UI.

Trong Bố cục in, chọn **Tạo khung từ ảnh…**, mở ảnh, kiểm tra bản xem trước rồi chọn **Tạo bố cục**. Các ô được đưa vào trình chỉnh sửa; bấm Lưu để lưu mẫu. Không ghi đè ảnh thiết kế gốc. Giới hạn nhập 20 MB và 24 triệu pixel. Thiết kế được đặt đồng tỉ lệ vào khổ 2×6 hoặc 4×6, hỗ trợ ngang/dọc; tỉ lệ khác có khoảng trống bù, không kéo méo.

**Nhập khung ảnh** và **Tạo khung từ ảnh** dùng chung bộ đọc PNG, JPEG/JFIF, BMP/DIB, GIF, TIFF, ICO, JPEG XR (WDP/HDP/JXR). WebP/AVIF phụ thuộc codec Windows; có thông báo khi không đọc được. Chặn phần mở rộng RAW/HEIC và âm thanh/video. GIF/TIFF nhiều khung lấy khung đầu. Khi nhập khung, chuyển nội dung thành PNG để lưu chung bố cục, giữ alpha nếu định dạng nguồn có alpha. Chưa hỗ trợ SVG/PDF/PSD. Đã chạy kiểm tra WPF nhập PNG/JPEG/BMP/GIF/TIFF/WDP, giữ alpha PNG và chặn RAW/CR3/NEF/HEIC/MP4/MP3.

## Thứ tự nhận diện và màu

1. Trong suốt (`alpha < 12`) hoặc magenta (`R >= 180, G <= 90, B >= 180`), màu chuẩn đề nghị `#FF00FF`. Chỉ chọn nhánh này nếu tín hiệu đạt 0,1% tổng số pixel.
2. Ô màu phẳng sáng: kênh nhỏ nhất ≥120, chênh giữa kênh lớn nhất/nhỏ nhất ≤25. Gom vùng liên thông 4 hướng với sai lệch từng kênh ≤2 so với điểm đầu. Loại vùng chạm mép ảnh, độ kín dưới 60%, hoặc lỗ/họa tiết chiếm trên 3% diện tích đã lấp. Có thể nhận ô xám, trắng, kem; không nhận cả trang trắng thành ô.
3. Placeholder trời–cỏ khi không tìm được ô màu phẳng: trời `B >= 200, R <= 242, B-R >= 15, G >= R`; cỏ `G >= 140, G >= R+5, G > B+10`.

Lọc hộp nhỏ hơn 0,5% diện tích ảnh; tối đa 16 ô. Có nối khe nhỏ, gộp mảnh, tách các placeholder xếp dọc và ước lượng góc nghiêng. Đây là quy tắc màu/hình học, không phải mô hình AI và không nhận diện được mọi ảnh thiết kế. Họa tiết lớn có màu đúng ngưỡng có thể bị nhầm; vì vậy cần xem trước.

## Vùng quét và khử trắng

**Ô phẳng:** lấy màu trội bên trong ô, tìm vùng liên thông lớn nhất với sai lệch RGB mỗi kênh ≤12. Vùng quét mở rộng 1,5% cạnh ngắn, giới hạn 4–20 pixel; ô xoay dùng kích thước tăng 10%. Nới viền chống răng cưa theo màu, có chặn khi vùng tràn tới mép vùng quét. Không xóa toàn bộ màu trắng trong ảnh.

**Placeholder trời–cỏ:** quét trong hộp ô cộng lề 15% cạnh dài. Flood-fill từ biên để giữ vùng bên ngoài. Phần sáng bị bao kín (`min RGB >= 190`, chênh kênh ≤40) và các cụm trắng (`R,G,B >= 225`) phù hợp hình học được coi là mây cần khoét. Có co/nở mặt nạ và kiểm tra tiếp xúc với trời–cỏ; giữ các đảo họa tiết màu hoặc tối.

**White matte thủ công:** chỉ xử lý giao giữa vùng người dùng chỉ định và ô ảnh. Tách thành phần màu mực, trắng và bóng đen trung tính để đổi trắng thành alpha, giữ mực/bóng. Core đã chuyển công thức này; giao diện chọn vùng, hút màu và bút sửa của web chưa được chuyển.

## Khác biệt và trạng thái

- Mặt nạ lỗ ảnh được ghi vào overlay PNG, nằm trên ảnh khách. Không thay đổi file nguồn.
- Magenta chỉ bị khoét trong ô đã nhận diện (kể cả ô nghiêng), tránh xóa họa tiết cùng màu ở nơi khác trên tờ.
- Bộ nhận diện trả cả góc ô và hướng nội dung placeholder. Trình bố cục hiện nhập góc ô; chưa có thuộc tính hướng nội dung độc lập tương đương `contentRotation` của web.
- Chưa có nhập hàng loạt, bút khoét/vá hay chọn màu nhận diện tùy ý trong UI.

Kiểm chứng: 70 unit tests đạt; 4 kịch bản simulator đạt; build WPF không cảnh báo/lỗi. Đối chiếu trực tiếp hàm TypeScript gốc và C# trên 4 ảnh tổng hợp (magenta, trong suốt, xám, trời–cỏ): khớp số ô, hình học và số pixel aperture. Nhập các PNG đó qua bộ đọc WPF tạo được layout hợp lệ. Đây chưa phải benchmark trên toàn bộ template thực tế hoặc nghiệm thu bản in.
