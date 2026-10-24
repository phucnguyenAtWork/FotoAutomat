# Đối chứng Exposure / Monochrome — 13/10/2026

## Kết luận

Suy luận từ LAST, không optimizer, không đổi checkpoint hoặc app. Trên cùng 128 ca của loss ablation, thêm gain theo ảnh và Monochrome giảm macro MAE 17,600 → 7,542/255. Low-key giảm 46,079 → 4,430; bias luma encoded giảm +47,383 → +0,063/255. Đây là bằng chứng lỗi độ sáng toàn cục chiếm phần lớn sai số low-key trên fixture này; không phân biệt nguyên nhân canonicalization, conditioning hay quá trình tối ưu.

Chưa đạt gate, chưa phải LUT cố định hoặc chất lượng tương đương Capture One. Không quy MAE thành phần trăm giống màu.

## Phương pháp và khả năng tái hiện

- Dùng nguyên 128 entry validation đã lưu trong `loss-ablation-2026-10-12-001/experiment.json`: 8 source × 8 look × 2 reference. Không dùng test. Baseline từng ca khớp MAE lần trước tuyệt đối (max difference 0).
- Chạy CanonAdapter ở eval/no_grad, lấy output float trực tiếp. Giữ raw ngoài miền để đo gate; clamp chỉ khi chấm MAE hiển thị và xuất PNG.
- Exposure: giải mã sRGB, lấy median luminance tuyến tính với trọng số 0,2126 / 0,7152 / 0,0722; gain = median(reference) / median(output). EV = log2(gain), cap ±3 EV, deadband 0,1 EV, bypass khi median nhỏ hơn 1e-5. Các ngưỡng định trước, không fit target. Chỉ áp gain sau model.
- BW: mean(max RGB − min RGB) < 3/255 và percentile 95 < 6/255 trên reference. Không dùng tên look để quyết định. Nếu bật, chuyển output về luminance tuyến tính rồi mã hóa sRGB, nên ba kênh bằng nhau. Nhãn look chỉ dùng chấm điểm/phân nhóm.
- Target không tham gia ước lượng gain/BW. Chỉ dùng đánh giá; highlight mask là target mean RGB ≥ 0,9, skin mask lấy từ pack hiện có. Mask da là ước lượng, không phải ground truth.
- Đếm cải thiện/tệ hơn dùng tolerance 0,001/255 để loại nhiễu số học. Nhánh không có hiệu ứng trả nguyên tensor, tránh round-trip không cần thiết.
- Lưu plan, ngưỡng, checkpoint/config/manifest/script hash, từng ca, gain, detector, raw gate; xác minh model state và đầu vào không thay đổi.

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-ExposureMonochrome.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Benchmark-ExposureMonochrome.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\exposure-monochrome-2026-10-13-002
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Benchmark-FixedGainTransfer.py --benchmark .local\matchlook-data\exposure-monochrome-2026-10-13-002 --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth
```

Chọn output mới nếu chạy lại. Bản `001` là lần đầu; `002` thay thế sau khi sửa no-op round-trip và tolerance đếm ca. Bản cuối mất 59,65 giây CPU; phép thử bổ sung 19,89 giây. Ba kiểm thử đã qua: round-trip giữ excursion, known gain/black guard, BW/sepia/local color/near-gray. Đã xem ảnh đầu ra và ảnh mẫu; chưa có duyệt visual toàn bộ ca.

## Kết quả cùng 128 ca

| Nhánh | MAE /255 | MAE da | MAE highlight | Vi phạm ca–tiêu chí | Tốt / tệ / không đổi |
|---|---:|---:|---:|---:|---:|
| LAST | 17,600 | 18,459 | 12,895 | 134 | — |
| Exposure theo ảnh | 7,752 | 7,998 | 13,999 | 90 | 76 / 8 / 44 |
| Monochrome | 17,350 | 18,426 | 12,706 | 118 | 16 / 0 / 112 |
| Kết hợp theo ảnh | 7,542 | 8,000 | 13,810 | 74 | 89 / 8 / 31 |
| Exposure từ ảnh mẫu | 51,708 | 68,792 | 13,979 | 288 | 0 / 128 / 0 |
| Kết hợp từ ảnh mẫu | 51,746 | 68,799 | 13,979 | 272 | 0 / 128 / 0 |

Da có 128 ca hợp lệ; highlight 112, cùng mẫu số giữa các nhánh. Vi phạm đếm ca–tiêu chí, không phải số ảnh. Joint còn 26 vi phạm tỷ lệ giá trị kênh RGB ngoài miền, 32 excursion, 16 neutral, 0 BW; mọi nhánh vẫn không đạt gate.

| Look | LAST | Exposure | Exposure + BW |
|---|---:|---:|---:|
| neutral | 15,055 | 7,234 | 7,234 |
| warm | 9,066 | 8,882 | 8,882 |
| cool | 15,479 | 12,410 | 12,410 |
| pastel | 9,215 | 8,397 | 8,397 |
| film_matte | 26,250 | 7,197 | 7,197 |
| high_contrast | 11,883 | 6,183 | 6,183 |
| low_key | 46,079 | 4,430 | 4,430 |
| monochrome | 7,771 | 7,280 | 5,606 |

Joint có 8 ca tệ hơn: 1 warm, 5 pastel, 2 cool; mức tăng lớn nhất 2,364/255. Highlight tổng thể tệ hơn dù MAE toàn ảnh/da tốt lên. Không nên chỉ xét macro MAE để bật tự động.

## Gain cố định và nội dung reference

Nhánh cố định ban đầu chọn ảnh train đầu tiên theo ID (`0209aea06bf03e82c4fc413dcdfc9f1f5452aebd1580469ba2c538bd06261b5b`, confirmed-session-01), lấy gain từ model output của ảnh mẫu với mỗi reference. Gain được chuyển sang source validation tương ứng. 128 cặp reference/look khác nhau nên đó chỉ là kiểm tra chuyển từ mẫu sang một ảnh khác cho từng look, chưa phải dùng một gain trên nhiều source.

Vì vậy bổ sung phép thử **64 ca, cùng một reference cho mỗi look, cùng một gain trên 8 source validation**. Ảnh calibration không thuộc validation, không dùng target để tính gain.

| Nhánh 64 ca | MAE /255 | Vi phạm | Ca tệ hơn baseline |
|---|---:|---:|---:|
| LAST với reference cố định | 19,113 | 84 | — |
| Joint theo ảnh | 7,863 | 28 | 6 |
| Joint gain cố định | 51,679 | 136 | 64 |

Ảnh mẫu có nhiều người, nội dung khác ảnh validation và thuộc nhóm chụp khác; chưa có chứng nhận hiệu chuẩn cùng booth. Gain từ mẫu của nhánh 128 ca đều dương (+1,06 đến +2,89 EV), gây sáng quá mức khi chuyển. Không lựa lại mẫu theo điểm validation để che thất bại. Kết quả bác bỏ việc dùng tùy tiện một mẫu hiện tại, không bác bỏ mọi phương án gain cố định dưới hiệu chuẩn đúng.

Ngay cả nhánh fixed này, model vẫn suy luận LUT theo source. Chỉ gain được giữ cố định. Cần phép thử riêng đóng băng cả LUT để đánh giá preset `.cube` thực sự.

Stress estimator: thêm vùng nền đen/trắng chiếm 2/3 canvas reference (giữ nguyên ảnh gốc ở giữa), không chạy lại model. EV ước lượng lệch tối đa 2,075 EV; nền đen có thể kích hoạt bypass near-black. Đây là negative control cho phụ thuộc nội dung, không phải phép đo độ bền toàn model.

Detector bật đúng 16 reference monochrome và không bật 112 reference màu trong fixture. Kiểm thử vẫn cho thấy màu rất gần xám có thể bị nhận là BW. Sepia và mảng màu trong ảnh xám bị từ chối trong các ca tổng hợp đã thử; chưa phải độ chính xác trên reference thực tế.

## Bước tiếp theo

1. Giữ tách Exposure và BW như ứng viên hậu xử lý; giải quyết 16 neutral và 58 lỗi RGB còn lại, cùng hồi quy highlight, trước tích hợp.
2. Đánh giá ước lượng exposure theo vùng/độ tin cậy và nhiều mẫu đại diện; so với global median. Không thay bằng full histogram matching.
3. Khi tạo preset, cố định cả phép biến đổi màu rồi kiểm tra trên nhiều ảnh cùng camera/ánh sáng/cài đặt. Phải đối chiếu đơn vị và thứ tự xử lý với `ColorRecipe.Exposure`; gain tuyến tính hậu model không mặc nhiên tương đương trường Exposure ở app.
4. Người dùng đã chọn khóa camera/WB/flash và dùng đèn liên tục cùng flash. Không cần hỏi lại quyết định này; điều còn thiếu là dữ liệu chứng minh cấu hình đó ổn định trong phiên chụp.

Chưa cần fine-tune tiếp để kiểm chứng các bước trên. Các kết quả là benchmark phát triển synthetic, đã dùng validation nhiều lần, chưa có đánh giá blind test hoặc booth thực tế.
