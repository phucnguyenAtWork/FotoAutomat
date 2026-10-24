# Mục 2–3: contract cross-content và normalization/style — 12/10/2026

## Đã triển khai trong lượt này

`NormalizationStyle.py` định nghĩa bước chỉnh theo từng ảnh: encoded sRGB -> linear RGB -> nhân 2^EV và gain WB ba kênh -> encoded sRGB. Gain xanh lá bắt buộc 1 để không nhập nhằng overall gain với Exposure. Giới hạn +/-4 EV và gain 0.25..4 là giới hạn kỹ thuật, không phải chuẩn camera. Provenance chỉ nhận identity_default/synthetic_known/manual_verified; chưa có estimator tự động. Đây là gain correction trên linearized JPEG, không phải WB RAW theo phổ cảm biến/Kelvin thật.

Hàm normalize_srgb báo tỷ lệ linear RGB vượt miền trước clamp. Adapter có đường gọi `prepare(example, normalization=...)`: chuẩn hóa source trước khi trích đặc trưng, giữ reference làm desired appearance; từ chối correction gây clipping. Không chọn correction tự động và không đổi hành vi khi không truyền normalization. Adapter vẫn gọi Canonicalizer gốc; chưa chứng minh hai bước không dư thừa/đối kháng. Sẽ cần ablation trên ảnh có ground truth trước khi bật mặc định.

`reusable_style_metadata` lưu recipe/style riêng, input contract là encoded sRGB sau normalization; không lưu correction của source mẫu. Style có thể có artistic exposure riêng trong recipe; đó là creative intent, khác per-image normalization. Chưa xuất được residual .cube đã tách Exposure/WB từ model; metadata không chứng minh LUT do CanonCGT tạo đã disentangled.

`Build-NormalizationFixtures.py` tạo 8 ảnh procedural độc lập (hình học/bảng màu), 64 cặp (8 base x 8 recipes). Reference và source khác bố cục, cùng base rendering được kiểm soát; target = cùng công thức áp lên base source. Source còn nhận degradation EV/WB biết trước; correction inverse lưu riêng. Parent source/reference ở cùng split và không vượt split. Không dùng JPEG khách hàng để tạo nhãn trước-sau giả.

Các file PNG là fixture đã render thật, khác 792 entries manifest cũ. Tuy nhiên chỉ là kiểm chứng contract, KHÔNG phải bộ ảnh chân dung/cảnh quan đủ để fine-tune production. training_ready=false. Oracle renderer dùng công thức biết trước, không đọc reference để suy đoán look, không dùng CanonCGT để tạo target.

## Kiểm chứng và kết quả

Output `.local/matchlook-data/normalization-cross-content-001/manifest.json`: 64 cặp, 8 base, 8 recipes (neutral/ấm/lạnh/pastel/matte/contrast/low-key/BW); float max round-trip normalization error 8.9407e-8; oracle output sau correction có MAE 0/255 so với target đã lượng tử. Đây là kết quả phép biến đổi deterministic, không phải accuracy model hoặc 100% Match Look.

7 test NormalizationStyle PASS: identity, inverse EV/WB, clipping diagnostic, invalid gain, style metadata không mang correction, parent isolation, adapter nhận normalization và chặn clipping. Adapter dùng tensor tổng hợp, không cập nhật optimizer. 3 test TrainingPipeline hiện có PASS để kiểm tra thay đổi adapter không phá pipeline cũ. Không fine-tune CanonCGT, không cloud/API.

## Đối chiếu bốn mục sau lượt này

| Mục | Phần hoàn thành | Phần còn thiếu / mức cải thiện |
| --- | --- | --- |
| 1 | RGB/neutral/BW đã gate best checkpoint, test PASS ở lượt trước | Ngưỡng tạm thời; model vẫn fail, chưa sửa màu |
| 2 | Có cross-content fixture có target biết trước; parent split isolation; manifest 64 PNG tuples | Chưa có photographic cross-content training set hoặc evidence model học transfer; fixture chỉ giúp kiểm tra dữ liệu đúng |
| 3 | Có correction EV/WB theo từng ảnh, style metadata riêng và adapter nối correction trước SSL; test inverse/clipping | Chưa tự ước lượng EV/WB, chưa tách latent style CanonCGT, chưa residual LUT export; cải thiện là contract rõ và chặn correction phá ảnh |
| 4 | Có kiểm tra mask trước đó và inference 256/512px | Chưa thêm full-resolution/landscape benchmark trong lượt này |

## Bước tiếp theo

Không coi fixture thay thế dữ liệu ảnh thật. Để tiến tới train cross-content có căn cứ: chuẩn bị base rendering chung từ ảnh RAW/ungraded có quyền hoặc dữ liệu paired được phép dùng; nếu chỉ có graded JPEG, nghiên cứu self-supervised canonicalization và đánh giá riêng, không tự khẳng định biết preset gốc. Tiếp theo cần thí nghiệm normalization có tham số biết trước trên ảnh thực bị perturb có kiểm soát: bật/tắt correction, so target và da/highlight, rồi mới thiết kế estimator. Không lấy gray-world tự động làm đáp án WB: nền màu có thể làm lệch ước lượng. Báo người dùng trước khi fine-tune.
