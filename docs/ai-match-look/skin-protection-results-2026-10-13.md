# Kết quả bảo vệ vùng da — 13/10/2026

Thực hiện bởi Claude (Opus) theo [protocol đăng ký trước](skin-protection-protocol-2026-10-13.md). Output: `.local/matchlook-data/skin-protection-2026-10-13-001/` (summary, cases, raw-cases, key-cases, plan JSON; bản sao protocol; ảnh cool kèm bản đồ khóa màu và mask). 184,2 giây CPU, 0 optimizer step, state model và hash đầu vào không đổi, không dùng test. 3 unit test của `Test-SkinProtection.py` đều qua.

## Kiểm tra đúng đắn

- `joint_mapped` và `norm_a10` tái hiện `exposure-normalized-correction-2026-10-13-001` trên mọi metric: sai số tối đa **0**.
- Khớp đối chứng: `key_off` β_global = 0,697, per-case 0/128 không khớp (hợp lệ). `key_luma` β_global = 0,845, nhưng **per-case có 5/128 không khớp**, nên đối chứng của `key_luma` không hợp lệ theo protocol.
- **Recall khóa màu = 1,0 ở mọi ca.** Đã kiểm tra lý do: mask da của pack là mask nhị phân từ phân đoạn ngữ nghĩa (MediaPipe Face Landmarker + Selfie Multiclass, đã erode, `Prepare-FinetuneMasks.py`), không dựa trên màu. Mask chỉ phủ lõi da mặt (2–7% ảnh), và mọi pixel lõi này đều nằm trong dải màu da. Khóa màu không đọc mask, nên **không vòng lặp theo định nghĩa**. Nhưng vì recall = 1,0, trên mọi pixel được chấm `key_off` **trùng hệt** joint_mapped, nên **kết quả tương đương vòng lặp** trên vùng được chấm. Da 7,991, 0 ca > 2 và da cool 9,03 của `key_off` là hệ quả tự động, không nói lên chất lượng da. Tiêu chí 4 (da trung bình) về cấu trúc không thể đạt với `key_off`. Thực sự đánh giá được `key_off` chỉ có vùng da ngoài mask (cổ, tay, viền mặt; hiện chưa được chấm) và các metric ngoài da.
- Khóa màu bắt nhầm **19–43% vùng ngoài mask** (trung bình 33%): nền ấm, tóc, gỗ… cũng không được correction.

## Kết quả (sau mapping, /255)

| Nhánh | Macro | Neutral | Da | Highlight | Ca da > +2 | Δda max | Da cool | MAE cool | Tốt/tệ hơn | Vi phạm raw/mapped |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| joint_mapped | 7,545 | 7,234 | 7,991 | 13,85 | 0 | — | 9,03 | 12,41 | — | 74 / 16 |
| norm_a10 (ứng viên) | 6,371 | 4,528 | 8,038 | 6,84 | 41 | 10,63 | 14,74 | 12,92 | 82 / 46 | 181 / 13 |
| mask_off (tham chiếu) | 6,364 | 4,667 | 7,991* | 6,85 | 0* | 0* | 9,03* | 12,64 | 81 / 47 | 180 / 13 |
| mask_luma (tham chiếu) | 6,386 | 4,640 | 8,333* | 6,84 | 22* | 9,45* | 9,72* | 12,69 | — | 180 / 13 |
| **key_off** | 6,371 | 4,952 | 7,991 | 6,88 | **0** | **0** | **9,03** | **12,24** | **93 / 35** | 177 / 14 |
| key_luma | 6,349 | 4,755 | 8,333 | 6,80 | 22 | 9,45 | 9,72 | 12,26 | 92 / 36 | 175 / 14 |
| key_off uniform-global (β=0,697) | 6,276 | 4,757 | 7,397 | 7,59 | 26 | 7,72 | 12,90 | 12,60 | 90 / 38 | 134 / 15 |
| key_off uniform-per-case | 6,206 | 4,685 | 7,261 | 7,51 | 21 | 6,94 | 12,70 | 12,55 | 96 / 32 | 146 / 15 |

\* Metric da của `mask_*` là vòng lặp theo cấu trúc (khai báo trước); chỉ dùng làm tham chiếu.

Chênh lệch MAE theo look của `key_off` so với joint_mapped: cool −0,17 · film_matte −1,90 · high_contrast −0,70 · **low_key +0,44** · monochrome −2,15 · neutral −2,28 · pastel −2,15 · warm −0,49.

## Áp tiêu chí đăng ký trước

| Tiêu chí | key_off | key_luma |
|---|---|---|
| 1. Dev cũ (neutral, per-look, macro, da TB) | Đạt cả 4 | Đạt cả 4 |
| 2. Phân bố da (≤20 ca > 2, max ≤ 5, p95) | Đạt (0 ca, max 0) | **Trượt** (22 ca, max 9,45) |
| 3. Cool (da ≤ 10,03; MAE ≤ 12,91) | Đạt (9,03; 12,24) | Đạt (9,72; 12,26) |
| 4. Thắng hai đối chứng cùng độ lớn | **Trượt một mục:** da trung bình 7,991 cao hơn 7,397 / 7,261. Thắng số ca > 2 (0 so với 26 / 21) và da cool (9,03 so với 12,90 / 12,70) | Trượt (da TB) và đối chứng per-case không hợp lệ |

**Theo quy tắc đã đăng ký: không biến thể nào đạt cả 4 tiêu chí, nên không chọn.** α=1 (`norm_a10`) giữ nguyên trạng thái ứng viên dev. Không đổi quy tắc sau khi xem kết quả.

## Diễn giải (không thay đổi quyết định trên)

- `key_off` là biến thể duy nhất **loại bỏ hoàn toàn** hồi quy da **trên vùng được chấm** (theo cấu trúc, xem trên): 0 ca > 2 (so với 41), da cool trở về 9,03 (so với 14,74), cool toàn ảnh còn tốt hơn joint (12,24 so với 12,41), và có nhiều ca tốt hơn nhất (93/128). Đổi lại, neutral mất một phần lợi ích (4,95 so với 4,53, vẫn tốt hơn joint 2,28), và low-key tăng 0,44.
- Nó trượt tiêu chí 4 vì **tắt correction trên da cũng bỏ luôn các ca correction đang làm da tốt lên**. Đối chứng thu nhỏ đều có da trung bình tốt hơn (7,26–7,40) **nhưng** vẫn để lại 21–26 ca da tệ đi > 2 và da cool 12,7–12,9.
- Vậy đây là một **đánh đổi sản phẩm**, không phải câu hỏi kỹ thuật: "không làm da xấu đi ở bất kỳ ảnh nào" hay "da trung bình tốt hơn nhưng có ảnh da xấu đi rõ". Với booth chân dung, lựa chọn thứ nhất có vẻ phù hợp hơn, nhưng **người dùng cần quyết định**. Sau đó lượt sau phải đăng ký lại tiêu chí 4 theo quyết định đó, và đánh giá lại trên dữ liệu mới, không chọn lại từ lượt này.
- So với `mask_off` trên metric ngoài da: `key_off` **tệ hơn ở neutral** (4,952 so với 4,667, +0,29), có lẽ do bắt nhầm 33% vùng nền; **tốt hơn ở cool toàn ảnh** (12,24 so với 12,64).
- `key_*` là hàm theo màu **cho từng cặp (source, reference)**: correction `c` vẫn cần một lượt model self-reference trên mỗi source. Một bộ lọc `.cube` cố định dùng lại được **chưa được kiểm chứng** (thí nghiệm frozen trước đó thất bại do ảnh calibration khác điều kiện chụp). Chưa có `.cube` sẵn sàng cho booth.
- `key_luma` (giữ phần độ sáng của correction trên da) vẫn làm 22 ca da xấu đi. Phần **độ sáng** của correction, không chỉ phần màu, đang gây hại trên da.

## Giới hạn

Dev set dùng lại nhiều lần (8 source × 8 look × 2 reference), 256 px, mask chỉ phủ lõi da mặt, chưa phải dữ liệu booth hay blind test. Khóa màu là heuristic cố định từ tài liệu, chưa kiểm tra với nhiều tông da, ánh đèn booth hay look làm đổi màu da mạnh. Tỷ lệ bắt nhầm 33% ngoài mask cần được đo trên ảnh booth.

## Tái hiện

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-SkinProtection.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-SkinProtection.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --previous .local\matchlook-data\exposure-normalized-correction-2026-10-13-001 --protocol docs\ai-match-look\skin-protection-protocol-2026-10-13.md --output .local\matchlook-data\skin-protection-2026-10-13-001
```
