# Audit self-reference và neutral — 13/10/2026

## Kết luận

Đã chạy 24 self-reference (8 source validation × clean/dark_cool_jpeg90/dark_warm_jpeg70), thêm 16 neutral khác reference (2/source), trong 10,44 giây CPU. Không optimizer, không sửa checkpoint, không mở tham số backbone hoặc áp residual correction.

Self-reference có drift rõ trên tập này. Tuy nhiên, drift chỉ giải thích một phần residual neutral; reference vẫn ảnh hưởng đáng kể. Không thể chọn nhị phân “chỉ source” hoặc “chỉ reference”. Tương quan không cô lập nguyên nhân canonicalization và không chứng minh phép trừ residual sẽ cải thiện.

## Thiết kế

- Lấy đúng 8 source và 16 entry neutral từ experiment của loss ablation. Không dùng test. Tách self-reference thành diagnostic, không sửa manifest hay đưa nó vào training.
- Loader tái dùng hai augmentation hiện có. Sau augmentation đặt reference = source.clone(), mục tiêu đo drift = source thực tế đó. Không so augmented output với source clean. Cả hai ảnh đầu vào cùng augmentation.
- Không dùng nhãn look để chọn bất kỳ phép correction nào. Neutral chỉ dùng chọn nhóm kiểm chứng đã có. Self-reference không áp recipe mới.
- Đo raw float trước clamp: MAE, bias từng RGB, luma bias/luma MAE/chroma MAE, tỷ lệ giá trị kênh ngoài miền và excursion. Giữ thêm display MAE để phân biệt ảnh PNG.
- Mỗi metric đo toàn ảnh, skin mask hiện có và highlight (mean encoded RGB của source ≥ 0,9), giữ convention cũ. Vùng trống trả null. Highlight có thể khác giữa augmentation nên không suy diễn trực tiếp chênh lệch giữa các điều kiện từ trung bình vùng này.
- Đo riêng canonicalized, raw restyled và joint-mapped. Nhánh mapped dùng gain/BW từ reference và gamut mapping giống output-policy; postprocessing áp độc lập cho self và neutral.
- So clean self-reference với 2 neutral cùng source ở cùng giai đoạn raw hoặc mapped. Đo cosine residual theo pixel, cosine bias RGB, dấu bias từng kênh (bỏ kênh |bias| ≤ 0,1/255), khoảng cách residual. Tính Pearson theo 8 source, dùng trung bình hai reference/source để không coi 16 cặp là 16 source độc lập.
- Canonical tensor giống hệt giữa self và hai reference khác trên cùng source. Đây là kiểm chứng tính độc lập reference của bước đó, không phải bằng chứng canonical output đã đúng màu.

## Số liệu chính

MAE raw so với chính source đầu vào, /255:

| Điều kiện | Canonical toàn ảnh | Self raw toàn ảnh | Self raw da | Self raw highlight | Self mapped toàn ảnh |
|---|---:|---:|---:|---:|---:|
| clean | 12,516 | 16,215 | 17,352 | 13,178 | 6,246 |
| dark_cool_jpeg90 | 8,047 | 10,130 | 11,573 | 4,912 | 5,322 |
| dark_warm_jpeg70 | 7,083 | 24,176 | 26,696 | 25,926 | 7,377 |

Canonical là biểu diễn trung gian được restyler sử dụng; MAE canonical không phải tiêu chí chất lượng cuối hoặc bằng chứng lỗi auto-WB vật lý.

Bias RGB trung bình trên 8 clean source:

| Giai đoạn | R | G | B |
|---|---:|---:|---:|
| Canonical | −12,558 | −12,243 | −10,751 |
| Self raw restyled | +11,508 | +16,651 | +14,910 |
| Self joint-mapped | −7,961 | −2,248 | −3,574 |

Canonical giảm cả ba kênh; restyler đổi dấu bias trung bình; hậu xử lý lại đổi dấu. Vì vậy không thể lấy hiện tượng giảm đỏ ở PNG cuối để quy riêng cho canonicalizer.

| So self với neutral khác reference | Raw | Joint-mapped |
|---|---:|---:|
| Pearson bias R theo source (n=8) | 0,745 | 0,348 |
| Pearson bias G | 0,869 | 0,816 |
| Pearson bias B | 0,820 | 0,259 |
| Pearson luma bias | 0,860 | 0,690 |
| Cả 3 dấu RGB khớp | 10/16 | 9/16 |
| Mean cosine residual theo pixel | 0,669 | 0,759 |
| Mean MAE giữa hai residual /255 | 10,248 | 4,762 |

Raw neutral MAE trung bình là 15,565 (display MAE ở các báo cáo cũ khoảng 15,055 vì clamp). Joint-mapped neutral MAE là 7,234. Không lấy số raw và display làm cùng một đại lượng.

Ảnh hưởng reference trên cùng source: mean MAE giữa hai neutral output là **17,647 raw**, **6,210 mapped**. Thay đổi này không nhỏ so với residual đang chẩn đoán. Có cặp self/neutral raw cosine âm (−0,155); sau mapping một cặp chỉ 0,053. Báo cáo từng source giữ các ngoại lệ, không chỉ trình bày tương quan trung bình.

## Diễn giải và bước kế tiếp

1. Có bằng chứng về drift roundtrip khi source=reference. Không đồng nghĩa kiến trúc chính thức bảo đảm identity hoặc drift đó luôn là lỗi cùng dạng trên reference khác.
2. Có liên hệ theo source, nhất là bias G, nhưng reference và hậu xử lý cũng thay đổi residual. Chưa chấp nhận correction cộng/trừ trực tiếp pixel theo self-reference làm mặc định.
3. Thí nghiệm sửa phù hợp tiếp theo là ablation riêng: baseline joint-mapped so với correction ước lượng **chỉ** từ self-reference, trên toàn bộ 8 look chứ không riêng neutral, có giới hạn cường độ định trước. Báo gate raw trước mapping, da/highlight, và look bị mất phong cách. Target chỉ chấm điểm. Correction vẫn phụ thuộc source, chưa phải fixed LUT.
4. Chưa mở canonicalizer để train. Nếu cần, phải thiết kế đối chứng với head-only và thông báo trước khi chạy optimizer; canonical drift hiện tại chưa đủ lý do chọn riêng khối này.
5. Thí nghiệm 2-point `a·Y^g` của Opus được ghi nhận là kết quả âm do người dùng cung cấp (16 PNG, MAE 8,78→12,39, gamma thường chạm 2). Scratchpad chưa được đọc hoặc tái hiện; không coi số này so trực tiếp với float benchmark hiện tại và không lặp lại phép thử đó trong lượt này.

## Tái hiện và kiểm thử

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-SelfReference.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Audit-SelfReference.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\self-reference-2026-10-13-001
```

Chọn output mới nếu chạy lại. Hai kiểm thử đã qua: float excursion không bị clamp trong raw metric/vùng trống; vector trái hướng và Pearson không xác định khi variance bằng 0. State model và file hash không đổi. Artifact gồm summary, self-cases, neutral-cases, by-source JSON, HTML và ảnh của cả 8 source. Đã xem canonical đại diện; chưa duyệt visual toàn bộ.

Giới hạn: chỉ 8 source từ validation phát triển đã dùng nhiều lần, chưa phải blind test hoặc độ tin cậy quần thể; skin mask ước lượng; chưa full-resolution hoặc booth hiệu chuẩn. Không có triển khai vào WPF/preset và không có checkpoint mới.
