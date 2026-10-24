# Preprocessing review — 06/10/2026

Đã thực thi audit + tạo preview định hướng EXIF trên subset PST50 15 ảnh. Không chỉnh màu, không train, không sửa originals. Script: `tools/MatchLookData/Preprocess-Review.py`. Kết quả `.local/matchlook-data/pst50-preprocessing-001/review.json`, `review.csv`, `previews/`.

## 40 biến duyệt đề xuất, không phải 40 input của model

29 trường tự động:

| Nhóm | Số | Trường |
| --- | --- | --- |
| File và hình học | 7 | sha256, bytes, width, height, mode, png_bit_depth, orientation |
| Color metadata | 4 | icc_present, png_srgb_intent, png_gamma, png_chromaticity_present |
| Camera metadata | 5 | camera_make, camera_model, iso, exposure_seconds, f_number |
| Màu/độ sáng mã hóa | 6 | red_mean, green_mean, blue_mean, encoded_luma_p01, encoded_luma_p50, encoded_luma_p99 |
| Biên dải giá trị | 4 | near_black_pixel_pct, near_white_pixel_pct, channel_low_pct, channel_high_pct |
| Độ nét proxy/trùng | 3 | laplacian_variance_proxy, dhash, exact_duplicate_of |

11 trường cần xác nhận: domain, capture_group, session_group, look_id, color_pipeline_verified, source_target_alignment_verified, rights_evidence_reviewed, face_count, skin_mask_reviewed, tone_target_approved, split.

path/role/case_id là khóa liên kết, flags là trạng thái xử lý; không tính vào 40. Số 40 là schema audit v1 của FotoAutomat, không phải số biến bắt buộc của PST50/CanonCGT. Một số trường như ISO không có không làm ảnh vô dụng cho Match Look, nhưng không thể gán nhãn tone camera đã xác minh.

## Kết quả đã đo

- 15/15 decode được, PNG 8-bit, không trùng byte. SHA256 đối chiếu provenance trước xử lý.
- 15/15 không có ICC; không có sRGB intent hoặc gamma trong metadata Pillow đọc được.
- Không ảnh nào cung cấp camera model, ISO hoặc exposure trong metadata đọc được. Không suy đoán những giá trị này.
- 5/5 cặp source-target cùng kích thước; điều này chưa xác nhận alignment pixel.
- 5 cặp được dHash gợi ý gần giống; chỉ đưa duyệt, không xóa vì source-target khác màu là dữ liệu cần giữ.
- 0 ảnh bị cờ biên sáng/tối vượt 1% channel samples theo proxy preview. Không có nghĩa ảnh gốc không clipping.
- Tất cả còn color/rights/labels review. Quyền dataset đã có nguồn CC-BY-4.0 nhưng quy trình duyệt asset chưa được tự động phê duyệt.

## Định nghĩa và giới hạn

Metrics tính trên bản RGB 256×256 BOX, giá trị 0–1. Encoded luma = 0.2126R + 0.7152G + 0.0722B trên giá trị mã hóa, **không phải luminance tuyến tính**. P01/P50/P99 là percentile. Near-black là mọi kênh ≤1/255; near-white mọi kênh ≥254/255; channel fractions đếm từng mẫu kênh. Resize có thể che clipping/blur cục bộ, do đó chỉ là cờ rà soát. Không dùng chỉ số này làm chứng nhận chất lượng full-resolution.

Laplacian variance là proxy trên kích thước cố định, chưa có ngưỡng loại ảnh; cảnh ít texture không đồng nghĩa sai nét. dHash Hamming ≤8 chỉ gợi ý gần giống; nhạy crop và có false positives. Metadata không có để null; presence không xác nhận profile đúng. Không dùng số RGB mean như nhãn tone máy ảnh.

Preview JPEG được sửa hướng EXIF và giới hạn cạnh 960, không có color transform. Chỉ dùng xem, không làm training master hay target metric. Source/target/reference original vẫn nguyên byte. Không histogram-equalize hoặc auto-WB reference. Chưa chuẩn hóa Rec.709 vì transfer cụ thể chưa xác nhận.

## Lệnh và kiểm thử

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Preprocess-Review.py --dataset .local/matchlook-data/pst50-starter --output .local/matchlook-data/pst50-preprocessing-new
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-Preprocess.py
```

3 tests PASS: đen/trắng và ảnh phẳng, đỏ bão hòa không bị gọi là trắng, schema 40 trường không trùng. Output phải mới và ngoài dataset. Không tự nhập CSV đã sửa vào manifest.

Tiếp theo: xác minh color pipeline upstream, duyệt alignment/nhãn, bổ sung kiểm tra chất lượng full-resolution nếu cần. Dataset chân dung do người dùng chọn sau; face_count/mask hiện chưa đánh giá, không điền 0 thay unknown. Không dùng 15 ảnh cảnh này để kết luận da cân bằng hoặc chọn checkpoint cho chân dung.
