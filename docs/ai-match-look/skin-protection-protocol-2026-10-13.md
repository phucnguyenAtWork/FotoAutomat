# Bảo vệ vùng da cho correction — protocol đăng ký trước (13/10/2026)

Thực hiện bởi Claude (Opus) theo yêu cầu người dùng, tiếp nối [kết quả headroom limiter](headroom-limiter-results-2026-10-13.md). Ghi **trước khi chạy**. Chỉ suy luận; không optimizer, không test split, không đổi checkpoint/gate. α=1 (`norm_a10`) vẫn là ứng viên dev đã chọn; protocol này không chọn lại α.

## Vấn đề

Correction chuẩn hóa exposure α=1 làm 41/128 ca da tệ hơn > 2/255. Ở cool: da 9,03 → 14,74. Lỗi này **không** do vượt miền RGB (H1 không được ủng hộ, limiter không giúp). Giả thuyết cần kiểm tra: correction đang đổi tone/màu trên chính vùng da.

## Ký hiệu

`y`, `x`, `c` (residual chuẩn hóa exposure α=1, cap ±0,15), `g0`, `bw`, `z(c') = apply(y − c', g0, bw)`, rồi gamut mapping, đúng như `Run-HeadroomLimiter.py`. `m` là mask da ước lượng có sẵn trong pack, dạng mềm [0,1]. `luma(c)` là phần độ sáng của correction (trung bình có trọng số BT.709, giống mọi script trước).

## Bốn biến thể

| Tên | Công thức | Loại | Nướng `.cube` được? |
|---|---|---|---|
| `mask_off` | `c' = c·(1 − m)` | theo không gian | Không |
| `mask_luma` | `c' = c·(1 − m) + luma(c)·m` | theo không gian | Không |
| `key_off` | `c' = c·(1 − k(x))` | theo màu source | Có |
| `key_luma` | `c' = c·(1 − k(x)) + luma(c)·k(x)` | theo màu source | Có |

**Khóa màu da k(x)** chỉ tính từ màu source (encoded sRGB, thang 0–255), không dùng target, mask hay tên look. Chuyển sang YCbCr BT.601 full range. `r(v; lo, hi) = clamp((min(v − lo, hi − v) + 3) / 6, 0, 1)` là ramp mềm rộng 6 đơn vị quanh mỗi biên. `k = r(Cb; 77, 127) · r(Cr; 133, 173) · r(Y; 30, 245)`. Dải Cb/Cr theo heuristic da kinh điển (Chai & Ngan 1999); dải Y loại vùng gần đen/trắng. Ngưỡng cố định, không tinh chỉnh.

Vì `c` và `k` đều là hàm theo màu của source, `key_*` vẫn là phép biến đổi theo màu, nên nướng được thành LUT. Lượt này không nướng.

## Vấn đề vòng lặp của biến thể mask (khai báo trước)

Metric da được tính trên **chính** mask `m`. `mask_off` tắt correction đúng trên mask đó, nên da gần như trở về joint_mapped **theo cấu trúc**. Kết quả da của `mask_*` vì thế là tự chứng minh, không phải bằng chứng. `mask_*` chỉ là **mốc tham chiếu**: nó cho thấy nếu bảo vệ da hoàn hảo theo mask thì phần còn lại (neutral, cool toàn ảnh, highlight) ra sao. **`mask_*` không đủ điều kiện được chọn.**

## Đối chứng cùng độ lớn

Với mỗi biến thể `key_*`: uniform-global β và uniform-per-case β_i, khớp `A_i = mean|z − z0|·255` trước mapping. Thuật toán khớp giữ nguyên lần trước (lưới 257 + bisection 40 bước, tolerance 1e-4/255, không dùng target/mask/look).

## Chẩn đoán khóa màu (báo cáo, không dùng để chọn)

Mỗi ca: recall = Σ(k·m)/Σm (khóa phủ bao nhiêu vùng mask da) và tỷ lệ dương tính ngoài da = Σ(k·(1 − m))/Σ(1 − m). Báo theo look.

## Tiêu chí cho `key_*` (đều phải đạt)

1. **Dev cũ:** neutral giảm ≥ 1,5 so với joint_mapped; không look nào tăng > 1,0; macro không tăng; da trung bình không tăng > 0,5.
2. **Phân bố da** (Δda so với joint_mapped): số ca > 2/255 ≤ 20 (một nửa của 41); Δda lớn nhất ≤ 5,0/255; p95 không cao hơn norm_a10.
3. **Cool:** da trung bình ≤ 10,03 (joint 9,03 + 1,0); MAE toàn ảnh ≤ 12,91 (joint 12,41 + 0,5).
4. **Lợi ích riêng:** thắng **cả hai** đối chứng cùng độ lớn: da trung bình thấp hơn, số ca > 2 ít hơn, da cool thấp hơn (sai số so sánh 0,001).

**Quy tắc chọn:** trong các `key_*` đạt cả 4 tiêu chí, chọn biến thể có ít ca da > 2 nhất; nếu bằng nhau thì chọn neutral thấp hơn. Nếu không biến thể nào đạt thì không chọn, và α=1 giữ nguyên trạng thái. Nếu chỉ `mask_*` cho kết quả tốt trên tiêu chí 1–3, ghi rõ: bảo vệ da cần đường triển khai theo không gian (cần segmentation lúc chụp), không phải `.cube`.

## Dự đoán

Nếu correction gây hại vì tác động lên màu da, `key_*` sẽ giảm mạnh số ca da > 2, nhất là ở cool. Rủi ro: khóa màu bắt nhầm nền ấm (gỗ, tường be), làm mất một phần lợi ích neutral; `key_luma` có thể giữ neutral tốt hơn `key_off`. Nếu `key_*` không giảm lỗi da mà `mask_*` lại có lợi trên tiêu chí ngoài da, thì khóa màu không bắt đúng vùng da của ảnh này.

## Đầu ra

Bản sao protocol, hash, bảng từng ca, tổng hợp theo look, chẩn đoán khóa màu, β/β_i, ảnh cool (Δda lớn nhất và trung vị) kèm bản đồ k. Ghi rõ: dev set dùng lại nhiều lần, mask ước lượng, ảnh 256 px, không phải dữ liệu booth.
