# Match Look trong Adjust — 04/10/2026

Mở **Bộ lọc màu → Adjust → Match Look** dưới histogram. Không còn tab Match Look riêng. Bấm thumbnail để chọn/drop reference, chọn ảnh cần chỉnh bằng **Đổi ảnh** của Adjust rồi **Apply**. **Use Selected Variant** lấy ảnh đang chỉnh làm reference; sau đó đổi ảnh đầu vào. Reference nhận ảnh khác cảnh, tỷ lệ, crop; không cần Paired Match.

Ảnh lớn, Before/After (Xem ảnh gốc), zoom/pan, histogram, Undo/Redo và các thanh dùng chung với Adjust. Apply tạo/cập nhật bản nháp Matched look; thông số có thể chỉnh tiếp ngay. Không tự phát hành hoặc đổi tone mặc định. Giải thích nằm trong tooltip và tài liệu, không có các đoạn benchmark dài trên UI.

## Thuật toán thực tế

`ReferenceMatchEngine` lấy mẫu độc lập tối đa 2048 pixel alpha 255 từ mỗi ảnh (tối thiểu 128). Sắp xếp bảy đặc trưng: R/G/B, luma, R−G, B−(R+G)/2 và HSV saturation quy về 0–255; lấy 33 quantile từ 0 đến 100%. Trọng số R/G/B mỗi kênh 0,4, luma 0,7, ba đặc trưng chroma mỗi loại 1,4. Tối ưu cùng ColorProcessor và Coordinate Descent dùng chung trong Core, với sáu lượt giảm bước. Không so pixel cùng tọa độ giữa hai ảnh, không tạo paired giả hoặc resize hai ảnh về cùng một tỷ lệ.

Hàm mất mát: RGB/chroma quantile RMSE + 0,2 × RGB RMSE thay đổi so với source + 120 × tỷ lệ kênh mới bị clipping (output ≥250 với input <245, hoặc output ≤5 với input >10), cộng ưu tiên rất nhỏ cho công thức đơn giản. Thêm điểm phạt hue drift quá 10° trên pixel có chroma ≥8, trọng số theo chroma tối đa 25, hệ số 0,6; và saturation tăng quá 0,05, hệ số 100. Hai điểm này lấy trung bình theo pixel source. Các hệ số là heuristic baseline, chưa hiệu chỉnh trên dataset camera thực tế; cũng có thể làm yếu look cố ý đổi sắc mạnh.

Reference dùng khoảng tìm bảo thủ hơn Paired: Exposure ±1,5; Warmth/Tint ±60; Contrast ±40; HDR/Saturation ±50; HSL Hue ±35, Saturation/Lightness ±50. Điểm giữa Curves ở 0,25/0,5/0,75 lệch tối đa 0,125 khỏi đường trung tính và cách điểm lân cận ít nhất 0,0625 theo trục Y, tránh đoạn phẳng. Quản trị vẫn có thể chỉnh rộng hơn trong Adjust.

Phương pháp thuộc hướng khớp thống kê phân bố màu; tham khảo khái niệm histogram specification trong [tài liệu Apple](https://developer.apple.com/documentation/accelerate/specifying-histograms-with-vimage). Đây là triển khai C# tối ưu recipe riêng của FotoAutomat, không dùng API Apple hay model của Capture One.

## Số đo và giới hạn

**RGB/chroma quantile RMSE** đo sai khác 231 mốc có trọng số của bảy đặc trưng. Giá trị RGB/luma/saturation ở thang 0–255, opponent chroma có thể âm. Before/After không kèm điểm phạt. **Không có tập check độc lập**, không phải pixel RMSE của ảnh paired, độ giống nội dung, màu da, phần trăm giống look hay Delta E. Origin/export mới dùng engine `reference-chroma-v2`; điểm mới không so trực tiếp với điểm RGB-only v1.

Không có skin mask/semantic segmentation, joint histogram, LUT fitting hoặc model AI Match Look. Windows face boxes được dùng ở chế độ ưu tiên vùng mặt bên dưới. Nền xanh, áo nhiều màu, khung trắng hay vùng trời chiếm nhiều diện tích có thể kéo tone theo nội dung thay vì bộ lọc. Các phân bố kênh giống nhau cũng có thể tạo cảm giác màu khác nhau. Giới hạn và điểm phạt giảm thay đổi cực đoan nhưng không đảm bảo màu tự nhiên hoặc giống máy ảnh. Luôn kiểm tra da và nhiều ảnh khác; chưa có bằng chứng đạt mục tiêu >90%.

Ảnh tham khảo không cho phép khôi phục duy nhất setting máy ảnh hay preset gốc. Reference là baseline mở điều kiện sử dụng và tạo điểm khởi đầu chỉnh tay; hướng semantic matching/AI vẫn tiếp tục theo [thiết kế Match Look](match-look-design.md).

Đã bỏ **Match method**, Paired Match và checkbox khớp vị trí khỏi UI. Màn hình chỉ chạy Reference Match. [Paired Match](paired-match.md) còn là baseline Core/benchmark nội bộ; không là điều kiện dùng Match Look.

## Kiểm tra

127 tests pass: Reference khác vị trí/tỷ lệ, khác số pixel, công thức kiểm soát, không thay input, giữ hình học source, hạn chế clipping khi reference trắng, dữ liệu lỗi, hủy, origin JSON cũ/mới và trường hợp histogram RGB giống nhau nhưng tương quan kênh sai. Tests Paired vẫn pass trong Core. Tests mới kiểm tra Impact, Normalize giữ nguyên, opacity layer và bật/tắt nhóm tìm thông số độc lập.

WPF đã chạy Reference với ảnh cần chỉnh 260×230 và reference khác cảnh 150×300; preview giữ 260×230, chuyển Adjust/lưu preset đúng nhãn Reference, đổi reference bỏ kết quả cũ. Đã tái hiện da đỏ bằng `DSCF9358.JPG` và `test.jpg` của người dùng, sau đó xem lại output v2: giảm sai lệch đỏ/bão hòa cực đoan, còn chưa giống tone tham khảo hoàn toàn. Điểm v2 của cặp này 63,71 → 35,74 không là nghiệm thu màu da/camera hoặc mục tiêu >90%. Ảnh gốc không bị ghi đè, dữ liệu kiểm tra không được đưa vào repository.

## Controls và nhiều ảnh

- **Normalize**: bật/tắt Exposure và White Balance riêng. Temp shift là điều chỉnh tương đối của engine, không phải Kelvin máy ảnh gốc.
- **Light & Contrast**: Contrast, Highlights/Shadows/Whites/Blacks và RGB Curve.
- **Color Adjust**: Saturation và HSL tám dải màu.
- **Color Tone**: Color Balance Shadows/Midtones/Highlights (fit từ v4) và Curves Red/Green/Blue.
- **Impact**: 0–100%, nội suy thông số màu về trung tính, giữ Exposure/WB. Preview dùng pipeline Adjust bất đồng bộ. Khi quản trị chỉnh tay hoặc đổi preset, Impact ngừng điều khiển kết quả cũ; Apply lại để tạo công thức mới.
- **Apply as new Layer**: màu nằm trong layer Full Photo với opacity bằng Impact; Normalize ở recipe nền. Có thể chỉnh layer trong Adjust.
- **Save as Preset**: mở hộp đặt tên và lưu riêng preset đang xem theo máy, không tự đổi tone mặc định.

Menu **··· → Add target images…** nhận 1–16 ảnh. Apply tìm recipe riêng cho từng ảnh theo cùng reference, bao gồm Normalize. Danh sách nhỏ dưới tool chọn kết quả để xem trên ảnh lớn. Cache giữ trong bộ nhớ; đổi reference/nhóm tìm thông số bỏ cache. Chưa phải xử lý xuất ảnh toàn độ phân giải hay bảo đảm nhất quán tương đương Capture One.

## Ưu tiên vùng mặt (v3)

**Face priority** trong menu ··· bật mặc định. Desktop dùng Windows FaceDetector cho cả reference và source. Core nhận các hộp chuẩn hóa; lấy 16×16 vị trí trong ellipse ở vùng giữa/dưới mặt (20–80% chiều ngang, 30–90% chiều cao), cân đều các mặt. Đây là vùng hình học, không phải mask da; vẫn có thể chứa môi, mắt, tóc hoặc vật che.

Khi cả hai ảnh có ít nhất 32 mẫu vùng mặt opaque, loss phân bố = 0,3 × sai khác toàn ảnh + 0,7 × sai khác vùng mặt. Các giới hạn và penalty v2 giữ nguyên. Engine origin là `reference-region-v3`; báo riêng Before/AfterRegionRmse, còn Before/AfterDistributionRmse vẫn chỉ đo toàn ảnh. Thiếu mặt ở một ảnh hoặc API không hỗ trợ thì dùng global v2. Không ép cùng vị trí, số mặt hay cùng người. Normalize và màu vẫn fit đồng thời, chưa là một model phân tách ánh sáng và camera look.

Với DSCF9358.JPG và test.jpg, Windows nhận 1 mặt ở mỗi ảnh. Điểm vùng mặt 34,80 → 8,38; điểm toàn ảnh 63,71 → 48,00 (global v2 trước đó là 35,74). Ưu tiên vùng mặt có thể làm điểm toàn ảnh cao hơn vì nền khác nhau. Đây là sai khác thống kê, không phải phần trăm giống look hay kiểm chứng màu da độc lập. Kết quả nhìn bớt đỏ nhưng chưa khớp toàn bộ tone reference.

127 tests đạt, gồm kiểm tra nền khác nhau không kéo sai màu vùng chủ thể như global, fallback khi thiếu vùng và bounds không hợp lệ. WPF đã kiểm tra tích hợp Adjust, cặp ảnh thật, Impact, chỉnh tay, lưu công thức, hủy và batch/chọn ảnh. Ảnh gốc không bị ghi đè; không đưa ảnh khách vào repository.

## v4 — 14/10/2026: Color Balance, nhiều reference, look-gram, xuất `.cube`

Mã engine mới: `reference-chroma-v4` (toàn ảnh) và `reference-region-v4` (ưu tiên mặt). Điểm v4 **không so được** với v2/v3, vì loss đã đổi.

**Color Balance.** Nhóm Color Tone fit thêm bánh xe Shadows/Midtones/Highlights, giống Capture One. Mỗi bánh xe được tìm dưới dạng offset Descartes (Amount·cos Hue, Amount·sin Hue), nên bộ tối ưu không phải vượt qua điểm 0°/360° của hue. Bánh xe được fit **trước** Curves R/G/B: tách tông nằm ở bánh xe (dễ đọc, dễ chỉnh), còn Curves chỉ nhận phần dư. Impact co giãn Amount và Lightness của bánh xe.

**Look-gram** (thêm vào loss): độ lệch màu trung bình (R−G và B−(R+G)/2) trong 8 dải độ sáng, trọng số theo mức hai ảnh cùng có pixel ở dải đó. Quantile từng kênh không phân biệt được "shadow xanh ngọc + highlight cam" với bản đảo ngược; look-gram phân biệt được. Metric Before/After vẫn là quantile RMSE như cũ; look-gram chỉ nằm trong loss.

**Điểm phạt tách theo da và không phải da** (thay đổi hành vi so với v2/v3).
- **Pixel màu da** (khóa YCbCr Cb 77–127, Cr 133–173, Y 30–245, thêm điều kiện chroma ≥ 30 để loại tường be / nhiễu gần trung tính): điểm phạt tính **trung bình riêng trên pixel da**, hệ số lớn (400 × tăng bão hòa vượt 0,03 + 3 × lệch hue vượt 5°). Đây là phương án A người dùng chọn: look không được làm da xấu đi. Ở v2/v3, điểm phạt chia trung bình cho toàn ảnh nên quá yếu: trong test reference rực rỡ, luật v2 để da tăng bão hòa +0,37 và lệch hue 20,6°; v4 còn +0,02 và 4,5°, trong khi nền vẫn tăng bão hòa theo look (optimizer giảm riêng dải màu da bằng HSL).
- **Pixel không phải da:** cho phép nhuộm màu. Lệch hue chỉ phạt khi chroma ≥ 24 (tăng dần tới 48); bão hòa được tăng tới mức chính reference cho thấy (0,05 + chênh lệch saturation trung vị reference − source). Ở v2/v3, hai điểm phạt này chặn look ấm/lạnh/tách tông trên cảnh gần trung tính.

Khóa màu da chỉ dựa vào màu (không cần mask theo vị trí), nên cùng quy tắc tái hiện được trong `.cube`. Nó có thể bỏ sót da quá nhạt hoặc da dưới đèn màu mạnh, và bắt nhầm vật có màu giống da. Cần kiểm chứng trên ảnh booth thật.

**Nhiều reference.** `ReferenceMatchEngine.FitReferences` nhận 1–8 ảnh của cùng một look trên nội dung khác nhau. Mỗi reference có trọng số bằng nhau. Capture One có cân theo độ giống nhưng không công bố cách tính, nên FotoAutomat không mô phỏng phần đó. Không cân theo khoảng cách màu tới source, vì cách đó tự ưu tiên reference vốn đã cần ít thay đổi. UI: chọn hoặc thả 1–8 ảnh vào ô reference; thumbnail hiện ảnh đầu và "+N".

**Xuất `.cube`.** Nút **Export .cube** nướng look đang chọn thành LUT 33³; xem [LUT export](lut-export.md).

**Kiểm chứng (tổng hợp, không phải ảnh thật).**
- 135 test đạt.
- Reference rực rỡ trên ảnh nửa da nửa nền: da tăng bão hòa +0,02, lệch hue 4,5° (luật v2: +0,37 / 20,6°).
- Tách tông do bánh xe tạo ra: khoảng cách 12,44 → 2,48. Bánh xe thu được shadows 199°/42, highlights 21°/35, so với đáp án 190°/45 và 35°/45.
- 3 reference cùng look ấm: khoảng cách giảm hơn 50%, ảnh nguồn ấm lên.
- MatchBenchmark (paired, có thêm bánh xe): đạt cả 3 ca.

Chưa kiểm chứng trên ảnh booth hay ảnh reference thật. WPF đã build nhưng chưa chạy trên màn hình sau thay đổi này.

**Giới hạn đã biết của v4 (15/10/2026):** reference mà **nội dung chiếm ưu thế màu** (biển, trời, rừng, tường màu) bị hiểu nhầm là look. Test tổng hợp: cảnh biển không chỉnh màu đẩy vùng trung tính của chân dung lệch +92 về phía xanh. Hướng sửa thử nghiệm v5a (điểm neo trung tính) sửa được ca này nhưng làm hỏng ca reference cùng loại nội dung, nên chưa đưa vào. Xem [v5a](ai-match-look/v5a-content-separation-2026-10-15.md). Từ 16/10, menu **···** có **Reference type**, mặc định **Auto (v5b)**: chân dung + reference toàn cảnh vật (không có mặt người) → v5a, còn lại → v4; có thể ép v4 hoặc v5a. Xem [v5b bước 1](ai-match-look/v5b-auto-reference-type-2026-10-16.md). Ngoài ra nên dùng nhiều reference cùng look trên nội dung khác nhau, hoặc reference có người hay vật trung tính, và kiểm tra bằng mắt.

**Cập nhật UI 17/10/2026**
- Panel Match Look dùng tiếng Việt.
- Mỗi reference hiện thành một ô riêng, có nút × để bỏ. Kéo thả ảnh hoặc chọn ảnh sẽ **thêm** vào bộ reference hiện có, tối đa 8 ảnh.
- Mô tả nhóm đã sửa cho đúng thông số được fit:
  - Color Adjust = Saturation + HSL;
  - Color Tone = Color Balance + Curves R/G/B.
- **Production (Release) ẩn menu Reference type và tên engine (v4/v5a)**: luôn dùng Auto, trạng thái hiện bằng lời thường ("reference cùng loại ảnh" / "reference phong cảnh"). Bản Debug hoặc chạy với `--dev` mới hiện các tùy chọn nghiên cứu.
