# Kiểm tra clipping đầu vào — 12/10/2026

## Đã làm

Thêm `known_source_loss` tùy chọn vào CorrectionPolicy.py. Khi caller có bằng chứng mất dữ liệu, policy bypass nguyên input và ghi reason `known_irreversible_source_loss`. Đây là tín hiệu provenance do caller cung cấp, không được hàm tự xác minh và không phải detector clipping JPEG. Không đổi các ngưỡng, không bật mặc định hay nối vào WPF/CanonAdapter strict guard.

Audit-ClippingPolicy.py: 12 source x4 biến thể (+0.3 EV float; +0.6 EV float, uint8, JPEG90), tất cả có mất dữ liệu do phép mô phỏng được ghi trước codec. Reference cố định từ session khác, SSL weights giữ nguyên, CPU 4 threads, cạnh dài 256px. Mất 38.153s. Không optimizer, grad None, hash state trước/sau trùng nhau.

So ba nhánh cùng 48 ca: bypass, guard chỉ xét correction, policy biết source loss. Đo sai lệch raw model output với output SSL nguồn nguyên bản, KHÔNG phải target reference. Source đã graded, không RAW ground truth.

## Kết quả

| Biến thể (12 ca mỗi hàng) | Bypass MAE /255 | Guard correction MAE /255 | Guard bỏ lọt source loss | Correction tệ hơn bypass |
| --- | ---: | ---: | ---: | ---: |
| +0.3 EV float | 16.0894 | 12.8708 | 12/12 | 2/12 |
| +0.6 EV float | 26.5894 | 45.3235 | 12/12 | 9/12 |
| +0.6 EV uint8 | 27.2517 | 45.1896 | 12/12 | 8/12 |
| +0.6 EV JPEG90 | 29.2700 | 39.4297 | 12/12 | 8/12 |

Lý do bỏ lọt: giảm Exposure sau clipping nằm trong miền RGB, nên chỉ kiểm tra range sau correction không chứng minh source còn đủ thông tin. Guard hiện tại là guard phép biến đổi, không phải guard lịch sử ảnh.

Với +0.6 EV float, skin MAE 27.5302 ->51.2207, highlight 26.1152 ->54.4903. Với JPEG90, skin30.0387 ->44.6584 và highlight25.9931 ->45.3592. Với +0.3 EV, correction giảm MAE trung bình (skin16.4754 ->13.3265). Vì vậy bypass không phải lựa chọn tối ưu cho mọi ảnh clip, chỉ là fallback bảo thủ. Nhánh có provenance bypass48/48, output đúng bằng bypass theo thiết kế; không phải AI cải thiện hoặc phát hiện đạt100%.

## Kiểm chứng

10 policy tests và8 normalization tests PASS. Regression mới: source loss đã biết vẫn bypass khi correction hoàn toàn trong range; ảnh trắng không có provenance không tự gán clipping; provenance phải boolean. Không xác nhận provenance của caller thay caller.

Đã xem overview.jpg: baseline SSL tự nó cũng có da quá sáng, và bypass không làm ảnh đã clip đẹp lại. HTML có nhãn tiếng Việt, minh họa4 ca source đầu, chưa browser QA. report.json và paired-summary.json đủ48ca, regional metrics cùng mẫu số12/cấu hình. Safe skin masks cache, highlight ROI encoded RGB mean>=.8; không detector mới/physical luminance. Không high-res/landscape/NUC benchmark.

## Quyết định

Không nới guard hoặc bật normalization tự động. Không gắn nhãn clip thật dựa riêng near-white/component. Phải tách risk lúc tạo correction và thông tin source loss đã biết. Nhánh known_source_loss phục vụ dữ liệu synthetic/processing history đã kiểm soát, chưa giải quyết JPEG bên ngoài.

Bước tiếp theo ưu tiên dữ liệu chụp thật có kiểm soát thay vì tiếp tục tối ưu ngưỡng trên12ảnh đã graded: xem booth-calibration-capture-checklist.md. Chuẩn bị ablation normalization vs augmentation sau dữ liệu và validation; thông báo trước fine-tune.

Lệnh từ repo: `.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Audit-ClippingPolicy.py` (yêu cầu output directory chưa tồn tại), rồi `Report-ClippingPolicy.py` để tạo paired-summary và nhãn HTML/overview. Dữ liệu khách hàng nằm .local, không commit.
