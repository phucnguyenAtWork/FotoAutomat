# Đặc tả limiter headroom — ghi trước khi chạy (13/10/2026)

Bổ sung bắt buộc cho `headroom-limited-protocol-2026-10-13.md` (mục "Ràng buộc cho lượt limiter kế tiếp" yêu cầu ghi chính xác công thức, vị trí áp dụng và cách xử lý baseline đã vượt miền trước khi chạy). Thực hiện bởi Claude (Opus). Mọi định nghĩa, nhóm A/B, đối chứng matched và tiêu chí khác lấy nguyên từ protocol của Codex; file này không thay đổi chúng.

## Ký hiệu

- `y`: output raw của model (LAST) với reference của ca; `x`: source.
- `c`: residual chuẩn hóa exposure của α=1, đúng `normalized_residual` trong `Run-ExposureNormalizedCorrection.py` (cap ±0,15 đã nằm trong `c`).
- `g0`, `bw`: gain và cờ BW ước lượng trên `y` (giống `joint_mapped`), giữ cố định cho mọi nhánh.
- `z(t) = apply(y − t·c, g0, bw)` với `t` theo từng pixel. Đây là output **sau gain/BW, trước gamut mapping**. `z(0)=z0` (joint), `z(1)` = norm_a10.

## Công thức limiter

Với mỗi pixel `p`:

1. Excursion của baseline: `E_p = max_k max(−z0_k, z0_k − 1, 0)` trên ba kênh k.
2. Miền cho phép của pixel: `[−E_p − 1e-6, 1 + E_p + 1e-6]` cho mọi kênh. Pixel baseline hợp lệ (E_p = 0) phải nằm trong [0,1]; pixel baseline đã vượt miền **không được vượt thêm** so với mức của chính nó. Correction không bị buộc phải sửa lỗi có sẵn.
3. Lưới `t ∈ {0, 1/32, 2/32, …, 1}` (33 điểm). `t_p` = giá trị lớn nhất trên lưới sao cho **mọi** điểm lưới từ 0 tới `t_p` đều nằm trong miền cho phép (tiền tố khả thi; không nhảy qua vùng vi phạm). `t=0` luôn khả thi theo định nghĩa.
4. `zL = z(t)` với `t_p` từng pixel; sau đó gamut mapping như các nhánh khác.

Limiter chỉ dùng `y`, `x`, `c`, `g0`, `bw`, không dùng target, mask da, tên look hay nhóm A/B. Lưới 1/32 là giới hạn độ phân giải đã biết. `t_p` không được làm mượt theo không gian. Nếu xuất hiện viền hoặc nhiễu theo pixel thì phải báo cáo, không sửa trong lượt này.

## Thứ tự chạy

1. Tái hiện `joint_mapped` và `norm_a025…norm_a10` từ cùng plan, rồi so từng metric với `exposure-normalized-correction-2026-10-13-001/cases.json` và `raw-cases.json` (sai số ≤ 1e-5). Lệch thì dừng.
2. H1: phân nhóm A/B theo `delta_skin` của norm_a10 so với joint_mapped (sau mapping), sau đó đo excursion trong mask da trên `z0` và `z(1)` đúng như protocol.
3. Limiter, đối chứng uniform-global β và uniform-per-case β_i (lưới 257 + bisection 40 bước, tolerance 1e-4/255, theo protocol).
4. Áp đủ tiêu chí dev, tiêu chí phân bố và tiêu chí H2 của protocol. Không thay đổi sau khi thấy kết quả.
