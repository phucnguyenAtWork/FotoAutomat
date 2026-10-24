# Refine — 02/10/2026

## Mở và sử dụng

Trong giao diện quản trị, chọn **Bộ lọc màu → Refine**. Tab thay thế thanh độ nét bản nháp bằng xử lý ảnh RGB thật trên cùng ảnh mẫu với Adjust và Retouch.

1. **Đổi ảnh** chọn JPEG/PNG/BMP/TIFF. Khi đổi sang ảnh khác, thông số và điểm xóa bụi trong bản nháp trở về trung tính. Ảnh gốc không bị ghi đè.
2. Mặc định không áp preset màu. **Apply color preset** bật bộ màu mặc định đã phát hành; tên preset ở tooltip. Retouch đã lưu cho ảnh này vẫn là đầu vào Refine.
3. **Navigator** hiển thị toàn ảnh và khung vùng đang xem. Bấm/kéo trong thumbnail để dịch vùng xem, lăn chuột zoom, kéo ảnh để pan, **Fit** xem toàn ảnh. **100%** là một pixel của ảnh preview tương ứng một DIP trong WPF; không phải ảnh đầy đủ độ phân giải từ camera. Focus xem crop 180×120 px ở giữa vùng đang xem, không điều khiển lấy nét camera.
4. Kéo thanh hoặc nhập số rồi Enter. **Before Refine** xem đầu vào trước Refine với cùng Retouch/lựa chọn màu. **Undo/Redo** hoàn tác thông số và điểm xóa bụi.
5. **Remove Dust · click spots** bật công cụ xóa bụi, bấm từng chấm trên ảnh. Spot size theo % cạnh ngắn. Show dust spots hiển thị vòng khoanh; vòng không ghi vào ảnh. Done trở về pan; Clear dust spots xóa các điểm đã chọn.
6. **Lưu Refine** đưa kết quả vào pipeline preview sau chụp. Bản chỉnh chưa lưu chỉ thay đổi phòng Refine. Reset Refine xóa hiệu ứng và điểm bụi trong bản nháp; lưu lại để phát hành việc reset.

## Các thanh xử lý

| Nhóm | Thông số / phạm vi | Thuật toán hiện tại |
| --- | --- | --- |
| Sharpening | Amount 0–500, Radius 0,1–3 px, Threshold 0–20, Halo Suppression 0–100 | Gaussian separable + unsharp mask; Threshold loại chi tiết lệch sáng nhỏ; Halo Suppression đưa overshoot về khoảng màu lân cận. |
| Noise Reduction | Luminance, Color, Detail 0–100 | Trung bình lân cận có trọng số chênh độ sáng; giảm nhiễu sáng/màu riêng. Detail cao giữ cạnh mạnh hơn. |
| Film Grain | Fine/Soft/Coarse Grain; Impact, Granularity 0–100 | Hạt đơn sắc xác định theo tọa độ, nội suy giữa các ô; Type và Granularity thay kích thước/mức hạt. Render lại không nhấp nháy mẫu noise. |
| Dust Removal | Spot size 0,2–10%; tối đa 1000 điểm | Thay vùng tròn bằng màu trung bình vòng lân cận, feather mép. Chọn thủ công, không phải AI nhận diện bụi. |
| Purple Fringing | Amount 0–100 | Giảm chroma tím/magenta tại vùng chênh sáng lân cận. Có thể ảnh hưởng họa tiết tím thật; cần so trước/sau. |
| Moiré | Amount 0–100, Pattern 1–32 | Làm trơn chroma có trọng số độ sáng; Pattern điều khiển bán kính. Không tái tạo chi tiết RAW hoặc khử mọi dạng aliasing. |

Amount làm nét, giảm nhiễu, Impact hạt, viền tím, Moiré mặc định **0**, không có điểm bụi; Radius/Detail/Granularity/Pattern không tác động khi Amount tương ứng bằng 0. Ô số dùng style riêng rộng 68, cao 28 DIP, dòng 34 DIP, chữ 12; cột công cụ 340 DIP.

## Lưu và pipeline

Thứ tự preview ảnh cuối: **ảnh gốc → Retouch đã lưu → bộ màu → Refine đã lưu**. Cả Adjust và booth mô phỏng dùng ảnh cuối này; live view bỏ Retouch và Refine. Từ 24/10, Grain/Sharpening chỉ còn ở Refine: recipe màu không tự áp nữa. Preset cũ (thường là preset Lightroom nhập vào) có Grain/Sharpening được `ShootingMode.Compose` chuyển sang Refine khi Refine chưa đặt giá trị đó (Grain × 0,75 → Impact, Fine, Granularity 0; Sharpening × 4/3 → Amount, Radius 0,8); nếu Refine đã có giá trị thì giữ Refine. Quy đổi gần đúng. Lightroom `Sharpness` không còn được nhập (capture sharpening của RAW; JPEG camera đã sharpen sẵn).

Workspace lưu `%LOCALAPPDATA%/FotoAutomat/Operator/refine-workspace.json`, gắn với đường dẫn ảnh mẫu. Chỉ một ảnh hiện tại, chưa có thư viện preset Refine hoặc tự xử lý mọi ảnh của phiên camera. Đổi ảnh không áp tọa độ bụi của ảnh cũ.

Mỗi lần xử lý lấy lại đầu vào, giữ alpha và pixel trong suốt; không sharpen/denoise tích lũy lên preview cũ. Render có hủy lần cũ khi kéo thanh. Không dùng mạng, SDK bên ngoài hoặc GPU cho module này.

## Giới hạn chất lượng và kiểm chứng

Hiện dùng preview chuẩn hóa EXIF/profile màu với cạnh dài tối đa 960 px, ảnh nguồn tối đa 40 triệu pixel và tệp 40 MB. WIC đọc ảnh nguồn trước khi kiểm tra kích thước và thu nhỏ. Radius/Threshold và hạt được tính ở độ phân giải preview; chưa có xuất full resolution, RAW, camera SDK hoặc profile ống kính. Xóa bụi thích hợp chấm nhỏ trên nền phẳng; không phải healing brush/content-aware fill, vùng lớn hoặc trên khuôn mặt/họa tiết có thể tạo mảng phẳng. Không tuyên bố tương đương thuật toán Capture One. Cần thử trên ảnh nhiều ISO, vải chi tiết và vùng sáng mạnh, cùng benchmark máy booth trước khi phát hành thương mại.

Portable tests bao phủ trung tính, alpha/ảnh gốc, sharpening/Radius/Threshold/Halo, nhiễu/Detail, grain ổn định, bụi chọn vùng, viền tím, giới hạn/hủy xử lý và lưu/đọc. WPF kiểm thử trên Windows bao phủ ảnh nguyên trạng, toggle màu, kéo Amount đổi pixel, zoom 100%, bấm điểm bụi/Undo/Redo, lưu/đọc, pipeline ảnh cuối/live riêng, đổi ảnh reset và ô số không bị style quản trị cắt chữ. Fixture ảnh đặt ngoài repo, không commit.

## Refine theo độ phân giải — 24/10/2026

- Tách hai bước: `RefineProcessor.Clean` (giảm nhiễu, moiré, viền tím, điểm bụi) chạy trên ảnh chính; `RefineProcessor.Finish` (sharpen rồi grain) chạy riêng cho từng đầu ra sau khi resize (ô in, ảnh tải về). `Apply` = Clean rồi Finish, giữ đúng thứ tự cũ cho preview quản trị.
- Bán kính giảm nhiễu và sharpen tính theo pixel của ảnh đang xử lý (lưới pixel đầu ra), nên phải đánh giá ở crop 100% và bản in; preview thu nhỏ không thể hiện đúng độ nét/nhiễu.
- Grain: kích thước ô hạt tính theo cạnh ngắn tham chiếu 1000 px và tọa độ tâm pixel quy về đơn vị đó, nên mẫu hạt và cảm giác hạt giống nhau ở preview, ô in và ảnh full (test: ảnh 500 px thu về 250 px tương quan > 0,9 với ảnh 250 px, cùng biên độ). Hệ quả: ở preview quản trị (cạnh ngắn ~640 px) hạt nhỏ hơn trước một chút.
- Mẫu hạt có seed theo ảnh (`RefineProcessor.SeedOf(PhotoId)`): mỗi ảnh khách một mẫu hạt, render lại cho cùng kết quả. Seed 0 (preview) giữ công thức hash cũ.
- Điểm bụi đã dùng tọa độ và bán kính chuẩn hóa; không thuộc preset chế độ (ShootingMode bỏ DustSpots).
- Chưa có: crop 100% trong trang Refine (đợt sau).
