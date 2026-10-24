# Kết quả headroom limiter, H1 và đối chứng matched — 13/10/2026

Thực hiện bởi Claude (Opus) theo [protocol của Codex](headroom-limited-protocol-2026-10-13.md) và [đặc tả limiter](headroom-limiter-spec-2026-10-13.md) đã ghi trước khi chạy. Output: `.local/matchlook-data/headroom-limiter-2026-10-13-001/` (summary, cases, raw-cases, h1-cases, limiter-cases, per-case-beta, plan JSON; bản sao protocol/spec; ảnh overlay excursion). 118,8 giây CPU, 0 optimizer step, state model và hash đầu vào không đổi, không dùng test.

## Kiểm tra đúng đắn

- Tái hiện `joint_mapped` và `norm_a025…norm_a10` so với `exposure-normalized-correction-2026-10-13-001` trên mọi metric số, cả mapped và raw: sai số tối đa **0**.
- Nhóm A/B tái hiện đúng: A = 41, B = 87.
- 4 unit test của `Test-HeadroomLimiter.py` đều qua. Có test limiter không bao giờ tăng excursion so với baseline, kể cả khi bật gain/BW.
- Matching: β_global = 0,8568 (sai số 2,4e-5/255, đạt). Per-case: 0/128 ca không khớp; β_i trong [0,238; 1], trung bình 0,878.

## H1: excursion trong mask da có đi cùng các ca da tệ đi không?

**Theo cách đọc đã đăng ký: không được ủng hộ.**

| Đại lượng (norm_a10, trước mapping, trong mask da) | Nhóm A (41) | Nhóm B (87) | A − B |
|---|---:|---:|---:|
| Tỷ lệ pixel excursion, trung bình | 0,158 | 0,108 | +0,049 |
| Tỷ lệ pixel excursion, trung vị | 0 | 0 | 0 |
| Tăng tỷ lệ so với joint, trung bình | 0,116 | 0,093 | +0,023 |

- Hiệu trung bình dương nhưng hiệu trung vị bằng 0: phần lớn ca **không có excursion nào trên da**.
- Trung bình trong-look (7 look đủ hai nhóm, trọng số ngang nhau) là **−0,027**, tức âm.
- Spearman giữa delta_skin và tỷ lệ excursion: 0,12 trên 128 ca, 0,14 khi gộp theo 8 source.
- **Cool (16/16 thuộc nhóm A):** không so A/B được. **9/16 ca cool tệ đi về da mà không có pixel da nào vượt miền.** Spearman trong cool là 0,65; theo protocol, con số này chỉ là chẩn đoán phụ.
- Low-key: 3 ca A đều không có excursion trên da. High-contrast: hiệu trung bình −0,002, gần như bằng 0.

Theo protocol: "Nếu cool tăng lỗi da nhưng phần lớn pixel da không excursion, ưu tiên protocol riêng để kiểm tra correction kéo ngược tông lạnh." Tình huống này đã xảy ra. Đây **chưa** phải kết luận "xung đột phong cách"; mask, tone curve và hue/saturation vẫn là các khả năng khác.

## Limiter và đối chứng matched (sau mapping, /255)

| Nhánh | Macro | Neutral | Da | Highlight | Cool | Low-key | High-contrast | Ca da > +2 | p95 / max Δda | Vi phạm raw / mapped |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| joint_mapped | 7,545 | 7,234 | 7,991 | 13,85 | 12,41 | 4,43 | 6,13 | 0 | — | 74 / 16 |
| norm_a10 (ứng viên) | 6,371 | 4,528 | 8,038 | 6,84 | 12,92 | 4,83 | 5,98 | 41 | 6,78 / 10,63 | 181 / 13 |
| **limiter** | 6,415 | 4,555 | 7,955 | 6,89 | 12,92 | 4,83 | 6,48 | 40 | 6,78 / 10,20 | **51** / 14 |
| uniform_global (β=0,857) | 6,277 | 4,558 | 7,689 | 6,98 | 12,76 | 4,48 | 5,81 | 36 | 5,71 / 9,20 | 166 / 15 |
| uniform_per_case | 6,289 | 4,426 | 7,809 | 6,68 | 12,78 | 4,83 | 5,73 | 37 | 5,77 / 9,15 | 181 / 13 |

Limiter giới hạn 9,2% pixel; t trung bình 0,952.

**H2, theo tiêu chí đăng ký: limiter không có lợi ích riêng.** Limiter thua cả hai đối chứng cùng độ lớn ở mọi tiêu chí da tổng (da trung bình, số ca > 2, p95, max). Ở cool và high-contrast, nó cũng thua về da và MAE.

**Tiêu chí phân bố của limiter:** không đạt. 40 ca > 2, vượt ngưỡng ≤ 30; có 1 ca nhóm B mới vượt > 2. Max và p95 không cao hơn norm_a10, và ba look nhạy cảm không tăng số ca. Tiêu chí dev cũ: limiter đạt, nhưng ba đối chứng cũng đạt. Như đã ghi, tiêu chí dev cũ không phân biệt được các nhánh.

**Điều limiter làm được:** giảm vi phạm raw trước mapping 181 → 51, đúng mục tiêu thiết kế. Nó không giúp da, nhất quán với kết quả H1.

**Đường đánh đổi (neutral MAE ↔ số ca da > 2):** norm_a10 bị uniform_per_case trội hơn trên cả hai trục (4,426 / 37 so với 4,528 / 41). Limiter cũng bị uniform_per_case trội hơn. β_i của uniform_per_case lấy từ độ lớn của limiter theo từng ảnh. Đây là một đối chứng, không phải phương pháp đã đăng ký, nên **không** chọn nó làm ứng viên từ kết quả này.

## Ba look nhạy cảm (trung bình 16 ca mỗi look, /255)

| Look | Nhánh | MAE | Da | Highlight | Tỷ lệ kênh vượt miền (raw) |
|---|---|---:|---:|---:|---:|
| cool | joint_mapped | 12,41 | **9,03** | 14,70 | 0,0005 |
| cool | norm_a10 | 12,92 | **14,74** | 13,11 | 0,0421 |
| cool | limiter | 12,92 | 14,71 | 13,03 | 0,0000 |
| cool | uniform_global | 12,76 | 13,87 | 13,10 | 0,0299 |
| cool | uniform_per_case | 12,78 | 14,30 | 13,00 | 0,0321 |
| low_key | joint_mapped | 4,43 | 5,66 | — | 0,0003 |
| low_key | norm_a10 / limiter | 4,83 | 4,92 | — | 0,0001 |
| low_key | uniform_global | 4,48 | 4,52 | — | 0,0001 |
| high_contrast | joint_mapped | 6,13 | 8,09 | 5,29 | 0,0561 |
| high_contrast | norm_a10 | 5,98 | 10,47 | 3,92 | 0,1914 |
| high_contrast | limiter | 6,48 | 10,33 | 4,91 | 0,0540 |
| high_contrast | uniform_global | 5,81 | 10,03 | 3,86 | 0,1816 |
| high_contrast | uniform_per_case | 5,73 | 9,27 | 4,07 | 0,1411 |

Ở cool, correction α=1 làm **da tăng lỗi từ 9,03 lên 14,74/255**. Limiter đưa vượt miền về 0 nhưng da gần như không đổi (14,71). Low-key không có highlight trong target nên ô highlight để trống.

## Theo source (norm_a10)

| Source | Ca nhóm A /16 | Δda trung bình | Tỷ lệ excursion trên da | Độ phủ mask da |
|---|---:|---:|---:|---:|
| 03788cc9 | 2 | −1,70 | 0,162 | 0,040 |
| 18053868 | 2 | −0,43 | 0,063 | 0,028 |
| 2064705f | 6 | −0,17 | 0,161 | 0,073 |
| 3633c855 | 7 | +0,52 | 0,133 | 0,033 |
| 4f0ff4c7 | 4 | −1,40 | 0,094 | 0,071 |
| 6eb79dc8 | 3 | −0,37 | 0,084 | 0,031 |
| 80ecc427 | 10 | +3,40 | 0,261 | 0,067 |
| 89edd23e | 7 | +0,52 | 0,034 | 0,021 |

Một source (80ecc427) chiếm 10/41 ca nhóm A. Mask da chỉ phủ 2–7% ảnh, nên metric da nhạy với một số ít pixel.

## Ghi chú phương pháp

- Tỷ lệ excursion trên da được **trọng số theo mask da mềm** (cùng cách metric da đang dùng). p95 và max dùng các pixel có mask > 0,5. Protocol định nghĩa tỷ lệ theo đếm pixel; hai cách chỉ khác nhau ở biên mask.
- Phép so trung vị A − B bị suy biến: phần lớn ca có excursion trên da bằng 0, nên cả hai trung vị bằng 0. Kết luận H1 dựa chủ yếu vào trung bình trong-look (−0,027) và việc 9/16 ca cool tệ đi mà da không vượt miền.
- `uniform_per_case` và `norm_a10` có **cùng tập** lý do vi phạm gate (raw 181, mapped 13; đã so từng phần tử). Gate là ngưỡng nhị phân theo ca–tiêu chí, còn metric liên tục vẫn khác nhau (ví dụ tỷ lệ vượt miền raw của high_contrast là 0,141 so với 0,191). Đây là trùng hợp do ngưỡng, không phải lỗi nối dây.
- **Diễn giải (chưa phải phát hiện):** trong cool, Spearman giữa Δda và excursion là 0,65, nhưng bỏ excursion bằng limiter không đổi da cool, và các đối chứng thu nhỏ đều lại tốt hơn. Có thể excursion chỉ là dấu hiệu của correction lớn, không phải nguyên nhân làm hỏng da.

## Đầu ra của protocol chưa được tạo

- Biểu đồ đánh đổi neutral ↔ số ca da > 2 **chưa được vẽ**. Dữ liệu có trong `summary.json` → `tradeoff_points` (kèm `dominated_by`).
- Ảnh overlay đã lưu theo quy tắc của protocol: `{look}-{max|median}-{source|reference|target|joint|norm_a10|limiter|uniform_global}.png`, `…-excursion.png` (pixel vượt miền tô đỏ) và `…-limiter-t.png`.

## Kết luận

1. Lỗi da tăng ở α=1 **không** chủ yếu do màu bị đẩy ra ngoài miền RGB, đặc biệt ở cool, nơi 9/16 ca tệ đi mà da không có excursion.
2. Headroom limiter sửa được lỗi miền màu (raw 181 → 51) nhưng không cải thiện da so với việc chỉ thu nhỏ correction. Không công nhận nó là cải tiến cho da.
3. α=1 vẫn là ứng viên dev đã chọn. Không chọn lại α hay β từ lượt này.
4. Bước tiếp theo theo protocol: protocol riêng cho correction trên vùng da, tách tone khỏi màu trên da. Phải đăng ký trước, có đối chứng matched tương tự, và đo việc giữ phong cách cool. **Ràng buộc triển khai:** mask da theo không gian không nướng được vào một `.cube` toàn cục (đã ghi trong `cpu-deployment-target.md` và `dry-run-2026-10-10.md`). Protocol nên đăng ký cả hai biến thể:
   - mask theo không gian;
   - giới hạn theo dải màu da (hue/saturation), vẫn là hàm theo màu nên nướng được thành LUT.

   Nếu chỉ biến thể theo không gian thắng, phải ghi rõ nó cần đường triển khai khác `.cube`.
5. Nếu cần giữ đầu ra hợp lệ, limiter có thể dùng như một bước an toàn miền màu (thay một phần gamut mapping), nhưng cần đánh giá riêng.

## Giới hạn

Dev set dùng lại nhiều lần (8 source × 8 look × 2 reference), mask da ước lượng, ảnh 256 px, chưa phải dữ liệu booth hay blind test. Lưới t có độ phân giải 1/32 và không làm mượt theo không gian; ảnh `*-limiter-t.png` cho thấy bản đồ t, chưa duyệt kỹ về viền.

## Tái hiện

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-HeadroomLimiter.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-HeadroomLimiter.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --previous .local\matchlook-data\exposure-normalized-correction-2026-10-13-001 --protocol docs\ai-match-look\headroom-limited-protocol-2026-10-13.md --spec docs\ai-match-look\headroom-limiter-spec-2026-10-13.md --output .local\matchlook-data\headroom-limiter-2026-10-13-001
```
