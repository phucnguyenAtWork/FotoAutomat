# Benchmark tách yếu tố đầu vào và look

## Thiết kế

Tiếp nối `reference-consistency-2026-10-12-001`, benchmark lấy 8 source đầu tiên từ kế hoạch cũ, 2 reference mỗi source và 4 look: neutral, film-matte, low-key, monochrome. Toàn bộ parent thuộc validation; chưa dùng test hoặc train thêm. Một session chân dung, chưa đại diện phong cảnh/camera khác.

Mỗi source/reference/look được thử trên 5 điều kiện, target và reference giữ nguyên:

| Điều kiện | EV tuyến tính | Gain RGB tuyến tính | JPEG |
|---|---:|---|---|
| clean | 0 | 1, 1, 1 | Không nén lại |
| ev_only | -0,6 | 1, 1, 1 | Không nén lại |
| wb_only | 0 | 0,9, 1, 0,95 | Không nén lại |
| jpeg_only | 0 | 1, 1, 1 | Quality 70, subsampling 2 |
| combined | -0,6 | 0,9, 1, 0,95 | Quality 70, subsampling 2 |

WB ở đây là probe cân bằng kênh, không phải Kelvin camera: chỉ giảm red/blue để tránh mất thông tin do clip, và cũng làm thay đổi độ sáng. EV tác động ở linear sRGB, khác exposure của recipe hiện có tác động ở encoded sRGB. Không coi chúng là cùng phép biến đổi hoặc phép đảo chính xác.

320 ca/model, cùng ca cho SSL gốc và LAST. Các perturbation tổng hợp được kiểm tra không tạo vượt miền tuyến tính trước encode/JPEG. Không có hiệu chỉnh camera thật hoặc estimator tự động.

## Đo lường

Target MAE và sai số vùng da đo trên đầu ra đã clamp. Luma bias là trung bình chênh lệch `0.2126R + 0.7152G + 0.0722B` ở encoded RGB, đơn vị /255, không phải độ chói cảnh vật lý. Bias dương nghĩa là đầu ra sáng hơn target.

Drift được so với đầu ra clean của chính model trên cùng source/reference/look. Không so trung bình trên tập ca khác nhau. Neutral identity gate chỉ áp cho ca clean; augmented neutral có target ảnh sạch, không target bằng source đã perturb. RGB excursion và fraction dùng output thô; monochrome chroma gate giữ nguyên.

Ghi từng weighted loss theo config train hiện tại để chẩn đoán độ lớn scalar. Scalar loss không phải độ lớn gradient, không đủ để kết luận loss nào gây lỗi. Không đổi loss hoặc ngưỡng trong benchmark.

## Chạy lại

Chạy từ FotoAutomat và chọn thư mục output mới:

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-LookDiagnostics.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Benchmark-LookDiagnostics.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --plan .local\matchlook-data\reference-consistency-2026-10-12-001\plan.json --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --output .local\matchlook-data\look-diagnostics-2026-10-12-001
```

Đầu ra gồm plan và hash, JSON từng ca và summary theo model/look/điều kiện, index.html minh họa một source. SHA checkpoint/manifest/plan được kiểm tra không đổi. Không có optimizer.

## Trạng thái

Đã hoàn tất 320 ca/model trong 134,2 giây trên CPU hiện tại. Bốn kiểm thử đã qua: geometry/range/input bất biến, EV không tạo cast trên ảnh xám, WB giữ green, signed luma và mask rỗng. Hash checkpoint/manifest/plan không đổi; không dùng test.

## Kết quả và kết luận

Mỗi ô dưới là trung bình trên cùng 16 ca (8 source × 2 reference), MAE /255:

| Look | SSL clean | LAST clean | LAST chỉ EV | LAST chỉ WB | LAST chỉ JPEG70 | LAST combined |
|---|---:|---:|---:|---:|---:|---:|
| Neutral | 30,02 | 15,06 | 10,21 | 11,65 | 19,97 | 8,87 |
| Film-matte | 21,69 | 26,25 | 12,19 | 20,20 | 30,58 | 13,88 |
| Low-key | 24,47 | 46,08 | 38,84 | 43,30 | 51,37 | 42,20 |
| Monochrome | 21,17 | 7,77 | 21,20 | 7,18 | 8,58 | 11,94 |

**Low-key sai cả khi ảnh sạch.** LAST có luma bias +47,38/255 so với target trên ca clean; film-matte +25,72/255. Không thể nói JPEG hoặc perturbation đầu vào là nguyên nhân duy nhất. Kết quả này xác nhận hiện tượng đầu ra quá sáng; chưa xác định do thành phần model, dữ liệu hay tối ưu nào gây ra.

**Đổi EV không có tác động thống nhất.** Low-key LAST có MAE thấp hơn khi source tối đi, nhưng vẫn sai lớn; monochrome ngược lại từ 7,77 lên 21,20. Drift monochrome với EV là 17,14/255. MAE giảm ở một perturbation có thể là sai lệch bù nhau, không chứng minh đã học normalization đúng.

**JPEG làm lỗi thêm nhưng không giải thích hết.** Film-matte LAST clean 26,25, JPEG70 30,58; low-key 46,08 → 51,37. Tỷ lệ giá trị kênh raw RGB vượt miền trung bình của film-matte tăng từ 2,89% lên 7,49%; neutral từ 7,06% lên 13,50%. Đây là tỷ lệ channel values, không phải tỷ lệ pixel. Không dùng dấu hiệu này để kết luận clipping vật lý trên ảnh gốc; perturbation đầu vào không tạo clipping tuyến tính.

**Loss cần kiểm tra gradient trước khi đổi trọng số.** Ví dụ monochrome clean LAST: weighted reconstruction 0,02953, skin 0,01139 và monochrome 0,0000935. Độ lớn scalar rất khác nhau, nhưng khác dạng loss nên không suy ngay rằng tăng hệ số BW sẽ sửa được lỗi. Cả 16 ca monochrome clean vẫn trượt chroma gate.

## Ưu tiên kế tiếp, chưa thực hiện

1. Audit gradient theo look và theo loss trên cùng head: norm, cosine giữa các nhóm, đóng góp của augmentation so với clean. Chỉ backward để đo, không optimizer step. Kiểm tra khả năng head cố định backbone có phân biệt được style tốt không.
2. Sau audit, chọn ablation nhỏ có đối chứng; không đổi đồng thời nhiều loss, kiến trúc và dữ liệu. Giữ gate và split, chưa mở lại test hoặc train dài.
3. PPL/nhánh recipe là hướng nghiên cứu sau khi có bằng chứng phù hợp; không coi thêm PPL là cách sửa chắc chắn các lỗi này.
