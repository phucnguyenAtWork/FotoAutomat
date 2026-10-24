# Xử lý ảnh sau chụp — plan đợt 1 (24/10/2026)

Trạng thái: **plan, chưa triển khai.**

## Đã chốt

- Chế độ chụp là một preset tổng: màu (recipe, có thể kèm LUT), Blur & Glow, Retouch, Refine. Khách chỉ chọn chế độ.
- Dữ liệu riêng từng ảnh không nằm trong preset: mặt/landmark/mask, điểm bụi, crop/ô.
- Grain/Sharpen chỉ còn ở Refine. Preset cũ được chuyển một lần; Lightroom `Sharpness` không map nữa.
- Retouch chỉ chạy công cụ có vùng xác định được. Không bao giờ dùng tọa độ mặc định hay của ảnh mẫu. Ảnh không có mặt không làm lỗi phiên.
- Nhận diện mặt: MediaPipe (Apache 2.0) qua ONNX Runtime CPU. Windows FaceDetector làm dự phòng. Không dùng YuNet (dữ liệu train WIDER FACE CC BY-NC-ND).
- Khách chọn khổ và số tấm từ 2 đến 8 trên cùng màn. Khổ 2×6 chỉ số chẵn (2, 4, 6, 8), in hai dải trên một tờ 4×6 rồi cắt. Giá theo số tấm để sau. **Đã làm 24/10**, kèm phát hành nhiều khung trong Thư viện khung; khách chọn khung cùng khổ ở bước Khung.
- Một mã QR cho cả nhóm: ảnh lẻ full độ phân giải camera đã áp chế độ chụp, khung, GIF, timelapse.
- GIF/timelapse lấy từ live view trong 10 giây đếm ngược; chất lượng theo live view (640–1280 px), chỉ áp màu dạng live view. Một GIF ghép các khúc của nhiều shot; một timelapse ngắn gọn cho cả phiên.

## Luồng xử lý

```
Ảnh gốc (giữ nguyên)
 → chuẩn hóa chiều xoay EXIF
 → nhận diện mặt (BlazeFace full-range → Face Mesh V2 cho từng mặt), lưu tọa độ chuẩn hóa theo PhotoId
 ├─ Đường in (ưu tiên, sau bước Khung): crop theo ô → resize về pixel ô in → Retouch → màu → Refine chi tiết → sharpen in → ghép khung → tờ in
 └─ Đường 26 MP (chạy nền sau khi có file in): Retouch → màu → Refine chi tiết → sharpen nhẹ → JPEG chất lượng cao cho QR
```

Hai đường dùng cùng snapshot, cùng kết quả nhận diện, thông số quy đổi theo độ phân giải. Nếu đo trên NUC thấy 26 MP đủ nhanh thì gộp: render 26 MP trước rồi cắt ra ô in.

## Công việc

### A. Preset và dữ liệu — đã làm 24/10 (trừ bảng `photo_jobs`, chuyển sang F cùng hàng đợi)
- `ShootingMode.Compose(look, retouch, refine)`: màu không còn Grain/Sharpening, Refine không có DustSpots, Notes ghi quy đổi. Retouch của chế độ = bộ đầu tiên đã chỉnh trên ảnh mẫu (`RetouchOf`), không mang tọa độ mặt.
- Retouch/Refine hiện vẫn là **một bộ chung** cho mọi chế độ (trang Retouch/Refine chưa chọn theo chế độ). Preset màu vẫn riêng từng chế độ.
- **Công tắc tính năng theo chế độ (24/10):** Bộ màu → Chế độ cho khách (trang riêng, mỗi bộ lọc một hàng: ảnh, tiêu đề, mô tả, tính năng): LUT, Adjust, Blur & Glow, Retouch, Refine. Chưa cấu hình = LUT + Adjust (như trước). Tính năng tắt thì phần đó trung tính (`ShootingMode.Compose(..., features)`); Grain/Sharpening cũ thuộc Adjust. Live view, ảnh vừa chụp, ảnh thẻ chế độ và ảnh màn chờ trên màn khách theo công tắc; snapshot phiên lưu công tắc. Retouch/Refine trên ảnh khách chạy khi có PhotoProcessingService (bước F); hiện chỉ thấy trên ảnh thẻ chế độ (render từ ảnh mẫu).
- Snapshot phiên lưu màu đã ghép, Retouch, Refine, ghi chú quy đổi. Phiên có `ShareToken` 128 bit (cột `share_token`, tự thêm vào DB cũ).
- Preview quản trị dùng cùng cách ghép, nên preset cũ trông như trước (trừ khác biệt nhỏ do quy đổi).
1. `SessionSnapshot` thêm `RetouchSettings` (một bộ cho mọi mặt) và `RefineSettings` (không có `DustSpots`).
2. Bỏ grain/sharpen khỏi `ExtendedColorProcessor.Finish`. Khi đọc preset cũ: `Grain` → `GrainImpact`, `Sharpening` → `SharpenAmount` nếu Refine của chế độ chưa có giá trị; đặt hai trường cũ về 0; ghi log quy đổi.
3. `LightroomPresetImporter`: bỏ `Sharpness`; `GrainAmount`/`GrainSize` → Refine.
4. `booth.db`: bảng `photo_jobs` (shot_id, kind = print|full, state, faces_json, output_path, error, attempts, updated_at). Phiên có `share_token` ngẫu nhiên tạo lúc `StartPaid`.

### B. Chọn khổ và số tấm — đã làm 24/10
5. Cùng màn Chọn khổ; snapshot lưu `Copies` của khách. `PrinterConfiguration.Copies` không còn dùng cho màn khách. Số tờ = số tấm / 2 với 2×6 ghép đôi, = số tấm với 4×6 (tính ở bước tạo file in).
6. Giá theo số tấm: để sau.

### C. Nhận diện nhiều mặt — đã làm 24/10 (MediaPipe ONNX + dự phòng Windows; cổ/Selfie Multiclass để đợt 2)
7. Port `IFaceAnalyzer` trong Core; bản MediaPipe trong Infrastructure (ONNX Runtime). Script chuyển `.tflite` → `.onnx` trong `tools/`, model + LICENSE/NOTICE trong thư mục `models/`, bộ cài kèm theo.
8. Nhận diện trên bản thu nhỏ khoảng 1920 px; nếu ảnh có mặt nhỏ, chạy thêm lưới vùng chồng nhau rồi gộp trùng (NMS). Xoay thẳng từng mặt theo điểm mắt trước Face Mesh.
9. Mức tin cậy → công cụ được chạy:
   - không mặt / mặt < ~6% cạnh ngắn: không Retouch;
   - chỉ khung mặt: da (Even Skin, Blemishes, Texture) trong elip mặt ∩ màu da;
   - Face Mesh đạt (góc nghiêng, tỉ lệ mắt/mặt hợp lệ): thêm mắt, quầng thâm, contour, răng (trong viền môi trong ∩ sáng, ít màu);
   - cổ: đợt 2 (cần Selfie Multiclass).
10. Ghi log từng ảnh: số mặt, công cụ đã chạy/bỏ qua.

### D. Retouch — đã làm 24/10 (một lượt, guided filter, công cụ theo mức nhận diện)
11. Một lượt cho mọi mặt, mask lấy max, không cộng/đè. Bỏ lỗi khi > 20 mặt: giữ 20 mặt lớn nhất.
12. Bán kính theo bề rộng mặt (bỏ kẹp 1–4 px); thay vòng lặp O(r²) bằng box/guided filter tách trục.
13. Dùng vùng mắt/môi từ Face Mesh khi có, thay elip vị trí cố định.

### E. Refine — đã làm 24/10 (Clean/Finish, grain theo cạnh ngắn 1000 px, seed theo ảnh)
14. Tách Refine: phần chi tiết (giảm nhiễu, bụi, grain) và phần sharpen theo đầu ra.
15. Grain: kích thước hạt theo cạnh ngắn, seed theo PhotoId. Bán kính giảm nhiễu/sharpen theo lưới pixel đầu ra.

### F. PhotoProcessingService — đã làm 24/10 (xem mục "Kết quả bước F")
16. Hàng đợi: preview chọn ảnh → job in (ưu tiên) → job 26 MP (nền). Một job nặng một lúc.
17. Ghi file tạm rồi đổi tên; xử lý lại luôn từ ảnh gốc; khởi động lại thì chạy tiếp job dang dở. Lỗi job in → phiên `NeedsReview`; lỗi job 26 MP → thử lại, không chặn khách.
18. Tạo file tờ in bằng `PrintSheetComposer` (gửi máy in thật vẫn ngoài phạm vi).

### G. GIF và timelapse
19. Trong 10 giây đếm ngược, lưu khung live view (JPEG) vào thư mục phiên.
20. GIF: một GIF lặp ghép các khúc ngắn của nhiều shot. Timelapse: ghép các lần đếm ngược thành một MP4 ngắn, tăng tốc cho cả phiên (Windows Media Foundation/`MediaComposition`, không kèm ffmpeg). Áp `ColorProcessor.ForLiveView` của chế độ.

### H. Đo trên NUC
21. Lệnh đo (`--benchmark-processing`) in thời gian và RAM từng bước ở cỡ ô in và 26 MP, với 1, 4 và 8 mặt. Chốt ngưỡng chờ cho khách sau khi có số.

## Ngoài phạm vi đợt 1
Website, upload, nội dung QR; gửi máy in thật; thanh toán thật; mask cổ/tóc/áo (Selfie Multiclass); crop 100% trong trang Refine.

## Nghiệm thu
- Ảnh mới, người khác, vị trí/chiều khác: vùng chỉnh theo mặt mới; ảnh mẫu không ảnh hưởng.
- Không mặt, mặt nghiêng, đeo kính, che mặt, nhóm 8 người: không chỉnh nhầm, không lỗi phiên.
- Ảnh gốc không đổi; render lại cùng kết quả (kể cả grain).
- Đường in và đường 26 MP cùng màu và cảm giác hiệu ứng.
- Preset cũ có grain/sharpen không bị áp hai lần.
- 2×6 không chọn được số lẻ; số tờ đúng.
- Restart giữa job, ảnh hỏng, hết dung lượng: trạng thái rõ, không mất ảnh gốc.
- Test portable cho processor, quy đổi preset, hàng đợi, chọn số tấm.

## Còn mở
- Giá theo số tấm (bạn gán sau).

## Kết quả bước F — 24/10/2026

- **Thứ tự xử lý:** mọi hiệu ứng chạy trên cả khung ảnh rồi mới cắt giữa vào ô (UniformToFill như màn Khung), vì vignette, mist/glow và grain tính theo cả khung. Đường in giải mã ảnh gốc (đã xoay EXIF, profile → sRGB, cùng bộ đọc ảnh mẫu) ở tỉ lệ `PhotoPipeline.PrintScale` để phần cắt vừa đúng pixel ô; đường full dùng tỉ lệ 1. Test: đường in khớp đường full đã thu nhỏ và cắt (chênh trung bình < 3/255).
- **`PhotoProcessingService`** (Infrastructure, port `IImageCodec`, `IFrameComposer`, `IFaceAnalyzer`):
  - In: chạy khi khách sang màn In. Mặt nhận diện một lần mỗi shot (bản 1920 px, lưu `shot_faces`), dùng chung cho đường full. Ghi `print/frame.png` (khung), `print/sheet.png` (tờ in, 2 × 6 ghép đôi theo cài đặt máy in) và `print/frame.fotolayout` (bản sao khung) trong thư mục phiên, qua file tạm rồi đổi tên.
  - Full: khi phiên Completed, xếp hàng mọi shot đã nhận và không bị xóa; một luồng nền ưu tiên thấp xử lý từng ảnh, tạm dừng khi có job in, ghi `final/<shot>.jpg`. Lỗi thử lại tối đa 3 lần.
  - `photo_jobs` trong `booth.db`: trạng thái, số lần thử, lỗi, thời gian, bộ nhớ. Khởi động: job in dang dở → Cancelled (khách đã đi, phiên NeedsReview); job full dang dở → chạy lại.
- **Màn khách:** màn In theo tiến độ job thật khi có ảnh camera; lỗi → phiên NeedsReview và hiện "Không tạo được ảnh in. Vui lòng liên hệ nhân viên." Không có ảnh camera (bản xem trước) thì giữ mô phỏng.
- **Đo trên máy dev (không phải NUC):** khung FH-01 (2 × 6, 3 ô, 300 DPI) từ ảnh gốc Fujifilm: ~2,9 s. Ảnh full ~12–14 s/ảnh, bộ nhớ tiến trình ~2,9 GB ở cuối mỗi job (đã ép dọn bộ nhớ sau mỗi ảnh; trước đó tăng dần tới 4,8 GB).
- **Trường hợp nặng (máy dev):** bật Blur & Glow (Mist 40, Glow 30): ảnh full ~21 s/ảnh, bộ nhớ đỉnh tiến trình 5,25 GB; job in chạy khi đang có ảnh full vẫn ~2,8 s (máy nhiều nhân; ảnh full đang chạy không bị dừng giữa chừng, chỉ dừng trước ảnh kế tiếp); GC dừng ứng dụng tổng ~414 ms cho 3 ảnh full (~140 ms mỗi ảnh, có thể làm giật live view của khách kế tiếp). Cần đo lại trên NUC i7-1360P/16 GB (bước H) trước khi chốt.
- **Chưa làm / giới hạn:** gửi lệnh in thật (số tờ = số tấm, chia 2 nếu 2 × 6 ghép đôi), upload và QR thật (ô SessionQr trên khung vẫn là hình QR giả của trình chỉnh, cần mã chia sẻ + địa chỉ web + bộ mã QR thật trước khi in thật), trang quản trị chưa hiện trạng thái job, màn Chọn ảnh/Khung vẫn chỉ xem trước màu (ảnh in có thêm Retouch/Refine), chưa chạy hết luồng khách bằng camera mô phỏng trên giao diện. Chế độ mặc định chỉ LUT + Adjust nên ảnh in chỉ có màu cho tới khi bật Retouch/Refine ở trang Chế độ cho khách. Có camera nhưng chưa phát hành khung nào (khung mẫu) thì màn In chạy mô phỏng và không tạo file. Chưa có chính sách giữ/xóa ảnh trên đĩa (mỗi phiên ~ảnh gốc + ảnh full).
