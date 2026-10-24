# Guard JPEG có fallback — 12/10/2026

## Triển khai và phạm vi

CorrectionPolicy.py thêm policy riêng, chưa thay strict guard mặc định trong CanonAdapter hay nối WPF. Đây là policy thử nghiệm, không tự suy đoán WB. Correction dùng thông số synthetic biết trước/sai số cố ý, không phải tham số model dự đoán.

Đo trước clamp: tỷ lệ kênh ngoài miền, mức vượt tối đa tuyến tính, thành phần liên thông 4 hướng lớn nhất theo tỷ lệ diện tích ảnh, tỷ lệ pixel ngoài miền trong union safe skin mask và highlight ROI. Ngưỡng thử nghiệm lần lượt 2%, 0.05 linear, 0.5% diện tích, 2% ROI. Numeric tolerance vẫn 1e-6. Các ngưỡng chưa được calibration hay chọn bằng held-out data.

Nếu vượt bất kỳ tiêu chí: trả nguyên input (bypass), ghi lý do. Nếu đạt: dùng correction đã clamp, ghi diagnostics. Không khẳng định diện tích/mức vượt chứng minh codec hoặc sensor clipping. Không phục hồi detail đã clip. Mask rỗng báo null; mask từ cache, không detector mới. Highlight dùng mean encoded RGB>=0.8, không scene-linear luminance.

## Kết quả cùng mẫu số

96 ca, 12 source development, 8 biến thể/source, 256px, CPU 4 threads, 47.662 giây tổng script. Source và reference khác session; giữ reference cố định mỗi source. Mỗi ca chạy bypass và policy với cùng model; policy có thể chủ động bypass. State hash không đổi, grad None, không train.

MAE bên dưới so raw model output với output SSL của source nguyên bản, KHÔNG phải độ giống reference. Mỗi hàng đủ 12 ca; policy mean bao gồm fallback, không chỉ ca được correction.

| Biến thể | Policy fallback | Bypass MAE /255 | Policy MAE /255 |
| --- | ---: | ---: | ---: |
| exact_float | 0/12 | 20.7637 | 0.0000 |
| exact_uint8 | 0/12 | 21.2315 | 1.1486 |
| exact_jpeg90 | 9/12 | 19.8565 | 16.0848 |
| exact_jpeg70 | 10/12 | 17.3193 | 15.6504 |
| ev_under_jpeg90 | 4/12 | 19.8565 | 10.7265 |
| ev_over_jpeg90 | 11/12 | 19.8565 | 18.6118 |
| wb_wrong_jpeg90 | 9/12 | 19.8565 | 15.9855 |
| ev_wrong_large_jpeg90 | 12/12 | 19.8565 | 19.8565 |

Policy giữ được đầu ra cho toàn bộ ca thay vì exception, nhưng JPEG90 vẫn fallback 9/12 và JPEG70 10/12. Vì vậy vấn đề JPEG chưa được giải quyết đầy đủ. Không dùng mức giảm MAE này làm phần trăm match-look hoặc quyết định phát hành. Positive EV error +0.3 fallback toàn bộ, policy bằng bypass đúng theo thiết kế.

## Kiểm chứng và giới hạn

7 policy tests và 8 normalization tests PASS. Policy tests có sparse excursion được clamp, connected excursion fallback, protected ROI, empty mask, invalid policy. Real audit giữ model frozen. HTML minh họa 8 biến thể source đầu, JSON đầy đủ 96. HTML chưa browser QA. Batch này chưa có nhánh mô phỏng mất highlight EV+0.6: cần bổ sung trước promotion. Chưa chạy high-res, CPU target NUC hay camera hardware.

## Bước kế tiếp

Thêm đối chứng clipped-source và đo riêng mức clamp mất dữ liệu; đánh giá phân bố excursion/lý do fallback trước khi chọn ngưỡng. Chuẩn bị checklist chụp chart/skin cho live view (đèn liên tục) và capture (đèn + flash), mỗi cấu hình camera riêng. Chưa fine-tune. Ablation augmentation chỉ chạy sau khi thông báo người dùng và dữ liệu/tiêu chí đủ sẵn sàng.
