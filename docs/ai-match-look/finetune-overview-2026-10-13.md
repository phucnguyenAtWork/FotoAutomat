# Tổng kết Match Look / fine-tuning CanonCGT — 13/10/2026

Viết bởi Claude (Opus) cho người dùng. Tóm tắt các tài liệu trong thư mục này; số liệu lấy từ báo cáo gốc được dẫn link. Không thay thế `phase-status.md`.

## 1. Train (fine-tune) đã giải quyết được gì?

**Chưa có model nào được duyệt.** Pilot fine-tune ([finetune-run-2026-10-12.md](finetune-run-2026-10-12.md)) chỉ train phần head 4.355 tham số, backbone đóng băng, 5 epoch, trên 384 ca validation:

| | SSL gốc | LAST (epoch 5) |
|---|---:|---:|
| Macro MAE /255 | 21,37 | 17,54 |
| Neutral / monochrome đạt gate | 0/48 / 0/48 | 0/48 / 0/48 |
| Low-key | 21,9 | **44,4** (tệ hơn) |
| Warm / pastel | 31,1 / 20,3 | 9,9 / 10,1 |

Không có `best.pth`, vì không epoch nào đạt quality gate. Các lượt sau không train thêm:
- [loss ablation](loss-ablation-2026-10-12.md): đổi loss BW/gamut, không nhánh nào tốt hơn LAST;
- [gradient audit](gradient-audit-2026-10-12.md): không thấy augmentation kéo ngược low-key.

## 2. Hậu xử lý trên output của LAST đã cải thiện gì?

Đo trên **128 ca dev** (tập con khác với 384 ca ở trên, nên không nối hai dãy số với nhau). Tất cả chỉ là suy luận, không train.

| Bước | Macro | Neutral | Low-key | Da | Highlight | Ghi chú | Báo cáo |
|---|---:|---:|---:|---:|---:|---|---|
| LAST thô | 17,60 | 15,06 | 46,08 | 18,46 | 12,90 | — | [loss-ablation](loss-ablation-2026-10-12.md) |
| + Exposure một gain + ép BW | 7,54 | 7,23 | 4,43 | 8,00 | 13,81 | Lỗi low-key gần như là lỗi độ sáng | [output-policy](output-policy-2026-10-13.md) |
| + gamut mapping | 7,55 | 7,23 | 4,43 | 7,99 | 13,85 | Đầu ra luôn hợp lệ | [output-policy](output-policy-2026-10-13.md) |
| + correction self-reference chuẩn hóa exposure α=1 | 6,37 | **4,53** | 4,83 | 8,04 | **6,84** | 41 ca da tệ hơn > 2, cool 9,0 → 14,7 | [exposure-normalized](exposure-normalized-correction-results-2026-10-13.md) |
| + bảo vệ da theo khóa màu (`key_off`) | 6,37 | 4,95 | 4,87 | 7,99* | 6,88 | 0 ca da tệ hơn trên vùng được chấm*; chưa được chọn | [skin-protection](skin-protection-results-2026-10-13.md) |

\* Kết quả da của `key_off` là tự động, vì khóa màu phủ toàn bộ mask được chấm (xem báo cáo).

## 3. Đã hiểu được gì (kể cả kết quả âm)

- **Low-key và film-matte sai chủ yếu vì độ sáng toàn cục.** Một gain theo reference sửa phần lớn. Nhưng một gain lại không sửa được **hình dạng** của drift.
- **Neutral sai** vì cặp canonicalizer + restyler không trả lại ảnh gốc ngay cả khi reference là chính source (drift self-reference khoảng 16/255). Drift chủ yếu là độ sáng; phần lệch màu nằm ở restyler.
- **Correction self-reference chỉ có tác dụng khi chuẩn hóa exposure trước.** Correction với gain cố định làm mọi look tệ đi; correction chỉ sửa chroma gần như không có tác dụng.
- **Lỗi da không do vượt miền RGB.** Limiter headroom sửa vượt miền (181 → 51) nhưng không giúp da. Correction đang tác động trực tiếp lên tone và màu da.
- **Kết quả âm đã ghi lại:**
  - fit đường cong 2 điểm;
  - correction chỉ chroma;
  - correction với gain cố định;
  - LUT đóng băng từ ảnh train;
  - limiter như cách sửa da;
  - dùng 24 ảnh validation còn lại làm holdout (đều đã dùng trong train).

## 4. Chưa giải quyết

1. **Gate neutral 3/255:** tốt nhất hiện khoảng 4,5.
2. **Bộ lọc `.cube` cố định dùng lại được:** correction hiện cần một lượt model self-reference trên mỗi ảnh nguồn, chưa có LUT đóng băng hợp lệ.
3. **Dữ liệu độc lập:** 128 ca dev đã dẫn dắt khoảng 8 quyết định liên tiếp, nên con số 6,37 là lạc quan. Chưa có holdout mới và chưa có ảnh booth.
4. **Đánh đổi da** (quyết định sản phẩm): "không ảnh nào da xấu đi" hay "da trung bình tốt hơn nhưng có ảnh xấu đi rõ".
5. **Chi phí CPU** trên NUC (thêm lượt self-reference), tích hợp WPF, kiểm tra nhiều tông da và ánh đèn booth.

## 5. Bước tiếp theo đề xuất

1. **Đã chốt 14/10/2026: người dùng chọn A**, không ảnh nào da xấu đi. Lượt sau đăng ký lại tiêu chí 4 theo lựa chọn này và đánh giá trên dữ liệu mới. Người dùng cũng nhấn mạnh lại mục tiêu: từ ảnh reference, Match Look phải **trích xuất được thông số chỉnh sửa** (recipe chỉnh được), không chỉ xuất một LUT.
2. **Chụp bộ ảnh kiểm chuẩn tại booth** theo cấu hình đã chốt (khóa exposure/WB/flash, đèn liên tục + flash; xem `booth-calibration-capture-checklist.md`). Một đợt chụp phục vụ ba việc: holdout độc lập, ảnh calibration cho LUT đóng băng, và kiểm tra khóa màu da dưới đèn booth.
3. Đóng băng phương pháp (α=1 + phương án da đã chọn), đánh giá **một lần** trên dữ liệu mới. Giữ 30 ảnh test cho nghiệm thu cuối.
