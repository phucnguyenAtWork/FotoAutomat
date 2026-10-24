# Correction đã chuẩn hóa exposure — đăng ký trước lượt chạy

## Thiết kế khóa trước khi xem kết quả

128 ca dev clean giữ nguyên plan của loss ablation; không dùng test/holdout mới. LAST giữ nguyên, eval/no_grad, không optimizer. Không chỉnh α hay ngưỡng dựa trên kết quả lượt này.

Với source x, self-output s và reference-output y:

1. Giải mã sRGB sang linear light, không clamp excursion.
2. `g_self = median(Y_linear(x)) / median(Y_linear(s))`. Không deadband. Nếu median ≤ 1e-5, không hữu hạn hoặc |log2(g_self)| > 3 thì bypass correction và ghi lý do; không cap gain rồi coi là đã chuẩn hóa.
3. `s_norm = encode(decode(s) * g_self)`; residual `r_norm = clamp(s_norm - x, -0.15, 0.15)` trong encoded RGB.
4. Ước lượng gain g0 từ y và reference bằng estimator cũ. Giữ nguyên g0 cho mọi α. Output trước mapping = gain/BW(`y - α*r_norm`); output hiển thị = gamut mapping. BW detector chỉ đọc reference.

Các nhánh: baseline joint_mapped; đối chứng fixed_a10 và reest_a10 của lượt 2; norm_a025/norm_a05/norm_a075/norm_a10. Cache self-output theo source; target chỉ dùng metric. Chưa đóng băng LUT hoặc nướng `.cube`.

## Tiêu chí dev

So với joint_mapped: neutral giảm ít nhất 1,5/255; không look nào tăng MAE quá 1,0; macro không tăng; da không tăng quá 0,5. Trong các nhánh normalized đủ cả bốn điều kiện, ưu tiên neutral giảm nhiều nhất; hòa thì chọn α nhỏ hơn. Không nhánh đạt thì không chọn.

Báo cả highlight, ca tốt/tệ và raw/mapped gate. Đạt tiêu chí dev chưa phải đạt gate chất lượng hoặc được tích hợp: raw gamut/neutral/BW tiếp tục theo policy cũ; mapped gamut hợp lệ là do phép xử lý đầu ra. Không nới gate. Tập dev đã dùng nhiều lần; α chọn được chỉ là ứng viên dev, cần dữ liệu độc lập để kiểm chứng.

## Kiểm tra bắt buộc

- Nếu s chỉ khác x bằng một gain tuyến tính, chuẩn hóa phải triệt residual gần sai số float.
- Identity và near-black/extreme-gain bypass; không che RGB ngoài miền bằng clamp ở bước chuẩn hóa.
- Baseline, fixed_a10 và reest_a10 khớp metric từng ca của lượt 2 với tolerance 1e-5.
- Median luminance sau chuẩn hóa khớp source trên các ca active; ghi sai số.
- Hash checkpoint/manifest/plan và state model không đổi. 30 test giữ nguyên, không đọc pixel test.

Trạng thái lúc đăng ký: chưa chạy; kết quả sẽ ghi riêng sau khi hoàn tất.
