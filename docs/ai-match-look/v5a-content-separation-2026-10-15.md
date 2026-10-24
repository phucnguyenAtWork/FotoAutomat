# v5a — tách look khỏi nội dung bằng điểm neo trung tính (15/10/2026)

## Vấn đề (đã chứng minh bằng test)

Test `Blue_scene_content_is_not_transferred_but_its_grade_is`: reference là cảnh biển (70% xanh bão hòa, 30% mây/đá trung tính) **không chỉnh màu**, ảnh nguồn là chân dung. **v4 đẩy vùng trung tính của chân dung lệch +92,5 về phía xanh** (B − R, thang 0–255). Recipe ra Warmth −50, Tint −60, Saturation +50: v4 bắt chước **nội dung** (biển xanh) như thể đó là look.

## Lịch sử thử nghiệm (15/10)

Đã thử 4 lượt chỉnh heuristic (look-gram theo phần trăm pixel ít màu nhất, trọng số tin cậy mềm, giảm trọng số quantile màu, mức cho phép theo điểm neo). Không lượt nào hội tụ: sửa test này thì vỡ test khác. Nguyên nhân đã chẩn đoán:
- Điểm neo theo phần trăm bị nhiễm bởi nội dung chiếm đa số dải sáng (biển ≈ 93% dải 3).
- Sai số bình phương (~13.000) áp đảo trọng số tin cậy mềm (~0,08); chuẩn hóa theo tổng trọng số còn khuếch đại thêm.
- Bão hòa toàn ảnh của một reference khác nội dung **không xác định được** là look hay là cảnh.
- Mức cho phép dùng chroma (/255), trong khi điểm phạt dùng saturation (tỷ lệ): lệch đơn vị.

## Lần thử duy nhất — hằng số ghi trước khi chạy

1. **Điểm neo trung tính có cổng cứng:** pixel chỉ là điểm neo nếu chroma (max − min) ≤ **80**/255. Lý do: trên dữ liệu tổng hợp, vật trung tính đã chỉnh màu có chroma khoảng 30–60, còn biển/trời bão hòa khoảng 150. Look-gram = màu trung bình của điểm neo theo 8 dải sáng. Dải không có điểm neo ở một trong hai ảnh có trọng số **0**. Trọng số mỗi dải = min(tỷ lệ điểm neo của dải ở hai ảnh); chuẩn hóa chỉ theo các dải có điểm neo.
2. **Quantile toàn ảnh chỉ học tone:** trọng số loss R, G, B, R−G, B−(R+G)/2, saturation = **0**; luma = **0,7**. Metric Before/After báo cáo giữ nguyên trọng số v2. Vùng mặt (khi có) giữ trọng số đầy đủ.
3. **Look-gram nhân 1** (như v4).
4. **Điểm phạt vùng không phải da đổi sang đơn vị chroma:** phạt tăng chroma/255 vượt mức cho phép; mức cho phép = 0,05 + max(0, chroma TB điểm neo reference − chroma TB điểm neo nguồn)/255. Điểm phạt da (phương án A) giữ nguyên.

**Tiêu chí đạt:** cả **136 test** đạt, gồm: hướng bánh xe tách tông (cosine > 0,9), "cùng phân bố giữ trung tính", bảo vệ da, và cảnh biển (lệch xanh < 4, look ấm vẫn truyền được). **Không đạt thì dừng:** giữ v4, đánh dấu Skip cho test cảnh biển kèm lý do, và ghi v5a là "đã nghiên cứu, chưa đưa vào".

**Giới hạn đã biết trước:** ngưỡng 80 dựa trên khoảng cách của dữ liệu tổng hợp. Trời hoặc biển lúc chạng vạng có thể có chroma thấp, nên ngưỡng chưa được kiểm chứng trên ảnh thật. Với một reference khác nội dung, v5a cố ý **không học mức bão hòa toàn ảnh và HSL** từ thống kê toàn ảnh. Phần đó cần nhiều reference, hoặc v5b (phân đoạn cảnh).

## Kết quả — không đạt, đã dừng theo quy tắc

Chạy một lần: **133/136 test đạt.** Patch lưu ở `.local/archive/v5a-attempt-2026-10-15.patch`. Code đã được khôi phục về v4.

| Kiểm tra | v4 | v5a |
|---|---:|---:|
| Cảnh biển không chỉnh màu → lệch xanh vùng trung tính chân dung (B − R) | **+92,5** | **−0,6** |
| Cảnh biển chỉnh ấm → chân dung ấm lên | không (81,1) | có (−13,2) |
| Tách tông: khoảng cách sau fit (bánh xe đúng hướng) | 2,48 | 3,66 (177°, 34°) |
| Bảo vệ da (reference rực rỡ): tăng bão hòa / lệch hue | +0,02 / 4,5° | −0,01 / 2,4° |
| 3 reference cùng look ấm, giảm bão hòa (khoảng cách) | đạt | **trượt**: 12,68 → 8,54 (cần < 50%) |
| Look biết trước, unpaired cùng loại nội dung | đạt | **trượt**: 16,04 → 11,30 (cần < 35%) |
| Khác số pixel / tỷ lệ ảnh | đạt | **trượt** |

**Chẩn đoán:** đây là **xung đột thiết kế**, không phải lỗi chỉnh hằng số.
- Reference **khác nội dung** (cảnh biển → chân dung): màu toàn ảnh là nội dung, phải bỏ qua. v5a làm đúng.
- Reference **cùng loại nội dung**: màu và bão hòa toàn ảnh chính là look, phải học. v5a bỏ qua nên trượt.

Muốn chọn đúng cách cho từng reference thì phải **biết nội dung của reference** so với ảnh nguồn: đó là v5b (phân đoạn cảnh). Một bộ trọng số cố định không phục vụ được cả hai trường hợp.

**Trạng thái ban đầu:** v4 giữ nguyên, v5a chưa đưa vào.

## Cập nhật 15/10: v5a là tùy chọn thử nghiệm (người dùng yêu cầu)

Người dùng muốn thử v5a trên ảnh thật trước khi xem v5b. v5a được đưa vào app dưới dạng **tùy chọn, mặc định tắt**, với cùng các hằng số đã ghi ở trên (không chỉnh thêm):
- Core: `MatchOptions.SceneReference`; mã engine `reference-anchor-v5a` / `reference-region-v5a`.
- UI: Match Look → menu **···** → **Scene reference (v5a)**. Trạng thái hiện "· Scene (v5a)" sau khi Apply.
- Tắt thì hành vi y hệt v4 (toàn bộ test cũ vẫn đạt).

Test: `Scene_reference_mode_does_not_transfer_blue_content_but_transfers_its_grade` (v5a, đạt) và `Default_mode_is_v4_and_still_has_the_known_scene_content_weakness` (ghi nhận lỗi của v4; khi v5b sửa được thì đổi test này có chủ đích). 137 test đạt, không còn skip.

**Cách dùng gợi ý:** bật khi reference là phong cảnh, kiến trúc hay cảnh vật (màu cảnh lấn át); tắt khi reference cùng loại với ảnh cần chỉnh (chân dung → chân dung). Đây vẫn là chọn tay. Tự nhận biết trường hợp là việc của v5b.
