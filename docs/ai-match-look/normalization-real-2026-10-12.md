# Normalization trên ảnh thật — 12/10/2026

Chạy `Audit-NormalizationReal.py`: 12 source JPEG development, mỗi source cố định một reference từ session khác; 4 perturbations/source = 48 ca. Long edge 256px, CPU 4 threads. Source/reference hash kiểm tra. Không train, state hash trước/sau bằng nhau, mọi grad None. 23.627s trong script; không benchmark booth hay full-resolution.

Phương pháp: trên ảnh sRGB đã graded, mô phỏng gain tuyến tính có EV/WB biết trước rồi áp inverse correction trước CanonCGT. Reference giữ nguyên. Đầu ra được so với **SSL chạy source chưa perturb**, không phải desired target. Đây là invariance kiểm soát, không phải accuracy Match Look. Không ước lượng tham số từ ảnh và không giả lập đầy đủ RAW/ánh sáng camera. Tensors giữ float sau perturb, chưa lượng tử lại JPEG nên trường hợp reversible là phép kiểm tra thuận lợi.

| Biến đổi | MAE output chưa correction /255 | Sau correction /255 | Source clipping |
| --- | ---: | ---: | --- |
| EV -0.6 | 32.1643 | 0.0000536 | 0/12 |
| Cast ấm, EV -0.3 | 9.2542 | 0.0000411 | 0/12 |
| Cast lạnh, EV -0.3 | 9.1910 | 0.0000448 | 0/12 |
| EV +0.6, clip highlight | 26.5894 | 45.3235 | 12/12 |

Đo trên raw SSL output, không clamp để che giá trị ngoài miền. Skin mask dùng safe mask từ region-stability-002, không chạy detector lại. Highlight ROI cố định từ source encoded RGB mean>=0.8, không phải scene-linear luminance segmentation. Case không có ROI nhận null, không zero. `region-summary.json` là mean theo case có ROI; không gộp diện tích các mặt hay claim face identity.

Với darkened ảnh, skin MAE giảm 36.1280 -> 0.0000630 và highlight 38.2322 -> 0.0000657 so baseline. Với clipped-brightened, skin tăng 27.5302 ->51.2207 và highlight 26.1152 ->54.4903. Điều này cho thấy correction biết trước tái lập baseline tốt khi thông tin chưa mất, nhưng không phục hồi clipping và có thể khiến SSL phản ứng xấu hơn. Baseline tự nó vẫn có lỗi màu/cháy; tái lập baseline không làm baseline đẹp hơn. Đã xem contact sheet ba biến thể đầu tiên của ảnh nhóm: correction reversible gần baseline; clipped branch khác rõ, baseline cũng quá sáng ở một số mặt.

## Lỗi đã sửa từ lượt đầu

Run normalization-real-001 chặn nhầm 10 ca inverse WB sát giá trị 1 do sai số floating point. Sửa NormalizationStyle: tolerance 1e-6 khi đếm out-of-range, đồng thời vẫn báo max excursion. Run002 không còn chặn nhầm các reversible cases. Tolerance chỉ dành cho số học, không khôi phục detail. Tám test normalization PASS gồm regression mới; không nới gain/exposure để phù hợp dữ liệu.

## Quyết định

- Giữ normalization là optional và chỉ nhận correction có provenance known/manual; chưa bật tự động.
- Không thêm correction oracle thành nhãn predicted training, không gọi 48 ca là training set.
- Với JPEG thật chỉ nhìn channel near-white không thể biết chắc clipping do sensor/export hay white object. Mức clipping ground truth chỉ biết trong mô phỏng này. Chưa có bộ phát hiện clipping thực tế tự động đủ căn cứ.
- Chưa đủ bằng chứng normalization+Canonicalizer tương thích với correction ước lượng sai. Cần stress test sai tham số, quantization/JPEG, rồi mới chọn estimator/normalization gate.

## Cross-check

1. Checkpoint RGB/neutral/BW: cơ chế đã nối lượt trước, calibration còn pending.
2. Cross-content: 12 real references để đánh giá visual và 64 procedural target fixtures; photographic training target vẫn pending.
3. Normalization/style: đã nối optional, biết correction tái lập được baseline khi reversible; vẫn thiếu automatic estimator, disentangled style LUT và benchmark robustness với sai tham số.
4. Nhiều mặt/nền/high-res: ablation có 12 ảnh gồm nhóm nhiều người, mask vùng da; chỉ 256px, chưa full-res/landscape.

Outputs `.local/matchlook-data/normalization-real-002/`: report.json 48 measurements, region-summary.json, HTML minh họa 12 ca đầu (3 source x4 variants), overview.jpg. HTML không phải toàn bộ 48 ảnh, JSON ghi đủ. Run001 giữ lại để audit.
