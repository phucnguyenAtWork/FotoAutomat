# Retouch — 02/10/2026

## Mở và sử dụng

Chạy ứng dụng Windows với `--admin`, mở **Bộ lọc màu → Retouch**. Tab này đã xử lý ảnh thật, thay thế các thanh bản nháp cũ. Ảnh thử dùng chung với Adjust.

1. **Đổi ảnh** chọn JPEG, PNG, BMP hoặc TIFF. Ảnh gốc không bị sửa.
2. **Detect faces** dùng MediaPipe (BlazeFace full-range + Face Mesh V2, ONNX, chạy CPU; xem `models/mediapipe/README.md`): tìm nhiều mặt và đặt sẵn tâm mắt/miệng theo landmark. Thiếu model hoặc lỗi thì dùng Windows FaceDetector (chỉ khung mặt). Thanh trạng thái ghi bộ nhận diện đã dùng. Nhận diện lại chỉ thêm mặt mới, không xóa các mặt/thông số đã sửa. Tối đa 20 mặt. Không thấy mặt thì chọn **Add face** và kéo một hình chữ nhật quanh mặt.
3. Chọn thumbnail mặt để chỉnh riêng từng người. Ba chấm trên ảnh là tâm mắt trái, mắt phải và miệng; mở **Feature alignment**, chọn công cụ và bấm đúng vị trí để căn lại. Left/Right là bên trái/phải của ảnh.
4. Kéo thanh hoặc nhập số rồi Enter. Mặc định preview không áp preset màu để tránh bộ màu cũ làm mờ ảnh ngay khi mở. Bật **Apply color preset** nếu muốn xem cùng preset mặc định đã phát hành (tên hiện trong tooltip). **Before Retouch** so sánh trước/sau Retouch với cùng lựa chọn màu. Lăn chuột phóng to, Pan kéo di chuyển, Fit về toàn ảnh.
5. **Protect areas** dùng Protect brush giữ chi tiết, Erase protection xóa vùng bảo vệ. Màu đỏ biểu thị vùng bảo vệ, không được ghi vào ảnh. Undo/Redo hoàn tác cả nét bút hoặc lần sửa thông số; Clear protection xóa mask của mặt đang chọn.
6. **Lưu Retouch** lưu vùng mặt, điểm căn chỉnh, mask và thông số cho ảnh hiện tại. Bản chỉnh chưa lưu chỉ đổi preview Retouch. Reset face đưa các thanh của mặt về trung tính, giữ khung mặt/điểm căn chỉnh/mask.

## Công cụ và thuật toán hiện tại

| Nhóm | Thanh | Xử lý |
| --- | --- | --- |
| Retouch Face Skin | Impact, Blemishes, Dark Circles | Cường độ da; giảm chi tiết lệch sáng cục bộ; nâng sáng dưới mắt. |
| Even Skin | Amount, Texture, Contouring | Làm mịn bằng trung bình lân cận có trọng số độ sáng; giảm/tăng chi tiết; dodge/burn tạo khối, không biến dạng khuôn mặt. |
| Include neck | Blemishes / Even Skin riêng | Mở rộng vùng xử lý xuống cổ; mặc định tắt. |
| Retouch Teeth | Brightness, Desaturation | Nâng sáng/giảm màu của pixel sáng ít chênh màu trong vùng miệng đã căn. |
| Retouch Eyes | Both / Left / Right, Iris, Sclera | Nâng sáng pixel tối/sáng trong vùng mắt đã căn; mỗi bên lưu thông số riêng. Both ghi cùng giá trị cho hai bên. |

Các thanh 0–100, riêng Texture −100–100. Impact điều khiển nhóm da; răng/mắt có cường độ riêng. Vùng da dùng khoảng YCbCr mềm bên trong hình ellipse mặt/cổ, loại vùng mắt/miệng. Mask bảo vệ giảm tác động của tất cả công cụ trên mặt tương ứng. Alpha được giữ nguyên; mỗi lần tính từ ảnh gốc, tránh tích lũy mất chi tiết.

## Liên kết ảnh và giới hạn

Thứ tự: **ảnh gốc → Retouch đã lưu → bộ màu**. Preview sau chụp của booth mô phỏng và Adjust dùng kết quả đã lưu. Live preview giữ ảnh gốc + bộ màu nhẹ, không chạy Retouch. Thanh làm đẹp của khách chưa điều khiển những thông số này.

Apply color preset chỉ đổi cách xem trong phòng Retouch, không thay đổi preset đã lưu hoặc preset của booth. Nếu ảnh cuối vẫn mềm do Clarity âm của bộ màu, chỉnh bộ màu ở Adjust rồi lưu lại. Không tự xóa thông số màu của người dùng khi mở Retouch.

Cấu hình lưu tại `%LOCALAPPDATA%/FotoAutomat/Operator/retouch-workspace.json`, gắn với đường dẫn ảnh mẫu. Đổi sang ảnh khác sẽ bỏ vùng chỉnh trong bản nháp; không áp tọa độ mặt cũ vào ảnh mới. Đây chưa phải preset làm đẹp tự nhận diện và áp vào mọi ảnh của một phiên camera thật. Chỉ lưu một workspace ảnh hiện tại.

Windows cung cấp **khung mặt**, không cung cấp landmark mắt/miệng trong module này. Các tâm ban đầu là ước lượng hình học và cần căn khi lệch. Chưa có semantic mask da, răng, tròng mắt, lòng trắng hoặc AI tẩy mụn; pixel sáng trong môi/kính có thể bị nhận nhầm. Vùng da theo màu có thể bỏ sót với ánh sáng phức tạp và các tông da khác nhau. Không tuyên bố tương đương Capture One; phải đánh giá trên ảnh booth nhiều người, kính, tóc, ảnh nghiêng và nhiều điều kiện sáng trước khi dùng thương mại.

Preview dùng pipeline ảnh mẫu hiện có: chuẩn hóa EXIF/profile màu, cạnh dài tối đa 960 px, ảnh nguồn tối đa 40 triệu pixel, tệp 40 MB. WIC đọc ảnh nguồn trước khi kiểm tra kích thước và thu nhỏ. Chưa xuất ảnh đầy đủ độ phân giải, RAW hoặc camera SDK. Nhận diện và xử lý chạy cục bộ, không gửi ảnh qua mạng và không xây hồ sơ nhận diện danh tính.

Cột công cụ Adjust/Retouch rộng 340 DIP, dòng thông số cao 34 DIP; ô số rộng 68, cao 28 DIP, chữ Segoe UI 12 và căn phải. Style số riêng không kế thừa margin 6/18 DIP của ô nhập trong cửa sổ quản trị, tránh cắt chữ. Đã kiểm tra vùng hiển thị số âm, số thập phân và Kelvin 50000 ở tỷ lệ bố trí 100%, 125%, 150% trên Windows.

Desktop target `net10.0-windows10.0.19041.0` để gọi WinRT; cần Windows 10 build 19041 trở lên. Khi FaceDetector không hỗ trợ máy, ứng dụng báo lỗi popup và vẫn cho Add face. Tham khảo API chính thức: [Windows FaceDetector](https://learn.microsoft.com/en-us/uwp/api/windows.media.faceanalysis.facedetector).

## Kiểm chứng

Portable tests kiểm tra trung tính, giữ ảnh gốc/alpha/nền, vùng cổ, mắt riêng, răng, bảo vệ/xóa bảo vệ, lưu/đọc và từ chối thông số không hợp lệ, hủy xử lý.

Kiểm thử WPF cục bộ trên Windows đã chạy nhận diện ảnh chân dung, quét lại không trùng, kéo Amount đổi pixel, đổi mặt, Undo/Redo mask, lưu/đọc, luồng ảnh cuối/live riêng, giữ ảnh gốc, đổi ảnh xóa vùng cũ và render giao diện. Ảnh khách/fixture kiểm thử đặt ngoài repo, không commit. Kết quả này không thay thế benchmark chất lượng hay kiểm thử camera thật.

Ảnh chân dung cắt để thử được nhận diện 1 mặt; ảnh ghép có 8 chân dung nhận được 7 mặt, ảnh trống không nhận mặt. Đây là bằng chứng cần giữ Add face và căn điểm thủ công, không coi nhận diện tự động là chính xác tuyệt đối. Tổng 104 portable tests và 4 simulator scenarios đã pass; Debug/Release WPF build không có warning/error.


## Thumbnail — 23/10/2026

Sửa thumbnail bị cắt vì kế thừa chiều cao nút 36px. Thẻ mặt rộng 112 DIP, cao tự động (149 DIP ở kiểm tra), vùng ảnh 96×112 DIP, Stretch.Uniform giữ toàn bộ khung mặt và tỷ lệ; nhãn mặt ở dưới. WrapPanel xuống dòng theo chiều rộng. Build/publish retouch-thumbnail-2026-10-23 thành công. Kiểm tra WPF offscreen hai tỷ lệ crop dọc/ngang, không mở Window; ảnh và nhãn hiển thị đủ. Không đổi vùng nhận diện hoặc xử lý Retouch.

## Retouch UI — 23/10/2026

- Hai checkbox vùng cổ dùng cùng nhãn Include Neck Area, vẫn điều khiển BlemishNeck và SkinNeck riêng.
- Iris/Sclera ở Both/Left/Right dùng -30…30, giá trị mới 0 ở giữa. Backend nhận giá trị âm để làm tối, dương làm sáng; 4 test kiểm tra cả hai mắt và hai vùng. Giữ khả năng đọc giá trị 0…100 đã lưu từ bản cũ; UI không tự ghi đè các giá trị cũ ngoài khoảng mới khi mở.
- Bỏ Feature alignment, các lựa chọn đặt mắt/miệng trên toolbar và ba chấm căn chỉnh. Dữ liệu vị trí feature đã lưu vẫn dùng trong bộ xử lý; mặt mới dùng vị trí tương đối mặc định, chưa phải landmark AI.
- Preview dùng cover theo tỷ lệ ảnh, clip phần rìa, zoom tối thiểu 1. Pan được giới hạn theo mép ảnh để không lộ nền; overlay/mask/brush dùng cùng hình chữ nhật cover. Fill chỉ thay cách xem, không crop ảnh lưu.
- Publish retouch-fill-2026-10-23 thành công; 196 tests đạt. WPF offscreen tại hai tỷ lệ khung kiểm tra pan/zoom, bounds ảnh khớp overlay và các slider mắt ở 0 với khoảng -30…30. Không mở Window hay chuyển focus; chưa kiểm tra cảm ứng vật lý.
## Nhận diện nhiều mặt và Retouch theo mức nhận diện — 24/10/2026

- `RetouchProcessor` xử lý mọi mặt trong một lượt: mặt chồng nhau không cộng hiệu ứng; mỗi pixel lấy kết quả của mặt làm pixel đó đổi nhiều nhất (không phụ thuộc thứ tự). Hơn 20 mặt thì giữ 20 mặt lớn nhất, không báo lỗi.
- Làm mịn da dùng guided filter dẫn bằng độ sáng (eps 28²), bán kính = bề rộng mặt / 60 (1–64 px): giữ cạnh mắt/môi/hàm, chi phí không phụ thuộc bán kính, cùng cảm giác ở preview và ảnh full. Ở cỡ preview kết quả gần như bản cũ; trước đây bán kính bị kẹp 4 px nên ảnh full gần như không mịn. Đo: 1280 px ~80 ms (cũ ~190 ms), 6000 px ~1,4 s, một mặt.
- Ảnh khách (`DetectedFace`, chưa nối vào luồng khách — bước F): không có tọa độ mắt/miệng mặc định. `RetouchProcessor.Plan` quyết định công cụ: chỉ khung mặt → da (Even Skin, Blemishes, Texture) trong elip mặt; landmark hợp lệ (hai mắt cách 25–75% bề rộng mặt, nghiêng ≤ 25°, miệng dưới mắt, điểm trong khung) → thêm mắt, quầng thâm, contour; có viền môi trong (Face Mesh) → thêm răng trong viền môi. Vùng cổ không chạy trên ảnh khách (cần mask da/áo). Mặt hẹp hơn 4% cạnh ngắn bị bỏ qua (trước đó dự kiến 6%; hạ xuống để không bỏ sót mặt trong ảnh nhóm 8 người ở 26 MP; chưa đo trên ảnh booth). Mặt người vận hành xác nhận trên ảnh mẫu vẫn dùng mọi công cụ.
- MediaPipe: dò toàn ảnh và lưới ô vuông 1/2 cạnh ngắn chồng 25% (ảnh lớn), gộp bằng NMS có trọng số; Face Mesh trên vùng mặt xoay theo hai mắt. Ảnh 1920 px: ~0,3–0,5 s. Thử trên 10 ảnh: mọi mặt tìm thấy đều đạt mức Mesh và bật đủ mắt/răng; ảnh ghép 4 khung (cùng một người) tìm đủ 4 mặt; ảnh chụp màn hình không có người trả về 0 mặt. Chưa thử ảnh nhóm đông người, kính, che mặt, ánh sáng booth thật.
