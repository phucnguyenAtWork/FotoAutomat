# CanonCGT CPU smoke test — 05/10/2026

Đã chạy thật 5 nhóm PST50, 1920×1080, CPU 4 threads, không train hoặc thêm face/skin processing. Đây là smoke test trên encoded RGB theo đường đọc demo upstream; chưa xác minh đầy đủ transfer Rec.709, chưa là benchmark chất lượng hoặc tỷ lệ chấp nhận.

## Nguồn và môi trường

- Upstream commit `229a7d3ec24af19ea5d97bb136f0317762bb9261`, Apache-2.0, checkout `.local/CanonCGT`.
- Python 3.12.14, PyTorch 2.14.1+cpu, torchvision 0.29.1+cpu; khác môi trường Python/PyTorch cũ của tác giả, chưa kiểm tra parity với bản đó. Venv `.local/canoncgt-env`; không sửa môi trường WPF.
- E2E.pth SHA256 `916f7ad5028d3fef51cf915bb51febc9508e7a378c0babe9ff3d3992b8c594f7`.
- SSL.pth trong Git chỉ 2 byte. Gói [weights chính thức liên kết từ README](https://drive.google.com/file/d/1SqzCXjdJ95TAhDYY9Z4TaQPuoqlEyfkT/view) có `SSL_updated_251111.pth`, SHA256 `5630c6dd5ceefa0d36d69ad298a5b87f1260dd02620a60af84cb4e259b4ad362` và E2E cùng hash như Git.
- ZIP SHA256 `ab460d12e0f826a507d54fc55ac6842ae935b1e83916d7259975e068c886cc51`. Weights ở `.local/canoncgt-official-weights/`. Chỉ nạp bằng `weights_only=True`.

## Adapter đã thêm, không phải training mới

`tools/MatchLookData/Run-CanonCgtCpu.py` bổ sung các import bị thiếu trong module SSL tại runtime, dùng CPU/eval/inference mode. Bỏ tải ImageNet initialization thừa rồi nạp toàn bộ weights suy luận bằng `strict=True`. E2E bỏ 57 `style_centroids.*` chỉ phục vụ training, không được gọi trong forward inference. SSL không bỏ tensor nào. Forward và các module/LUT upstream không chỉnh thuật toán. Không retouch, thêm skin mask, tự chỉnh exposure, hoặc hậu kỳ màu.

Xuất PNG full resolution bằng clamp [0,1] và round 8-bit; tất cả kết quả finite, tỷ lệ channel ngoài [0,1] bằng 0. Comparison/HTML dùng JPEG thumbnail nên không dùng đo metric.

## Kết quả thời gian

| Case | E2E lần đầu (s) | E2E chạy lại (s) | SSL chạy lại (s) |
| --- | --- | --- | --- |
| 1 | 3.233 | 0.317 | 0.292 |
| 2 | 5.341 | 0.368 | 0.370 |
| 3 | 6.888 | 0.368 | 0.361 |
| 4 | 9.784 | 0.366 | 0.360 |
| 5 | 7.383 | 0.367 | 0.359 |

Đo chỉ forward inference, không gồm imports/load weights/đọc/lưu ảnh. Chênh lệch lớn giữa lượt đầu và lượt lại chưa giải thích; không khẳng định model SSL nhanh hơn, không coi đây là latency p95 hoặc cam kết máy văn phòng. Chưa đo peak RAM.

Đã xem comparison cả hai model: output còn gần source ở nhiều cảnh; tone xanh ở case 1 và vàng/xanh ở case 3 chưa khớp target, E2E có ám tím ở case 4. Đây là nhận xét thị giác, chưa có % hoặc điểm Delta E. Chưa đủ kết luận model phù hợp sản phẩm. Cần kiểm tra preprocessing/config/checkpoint parity, thử mẫu upstream và reference-sensitivity trước fine-tune để tránh học bù lỗi tích hợp.

## Xem và chạy lại

- `.local/matchlook-data/canoncgt-ssl-cpu-002/index.html`: 5 hàng Source / Reference / SSL / Target.
- `.local/matchlook-data/canoncgt-e2e-cpu-002/index.html`: đối chứng E2E.
- Mỗi thư mục có `comparison.jpg`, `result-1.png` đến `result-5.png`, `report.json` (hash, version, timings, state loading).

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Run-CanonCgtCpu.py --repo .local/CanonCGT --dataset .local/matchlook-data/pst50-starter --weights C:/Users/Admin/Desktop/Work/FotoAutomat/.local/canoncgt-official-weights/SSL_updated_251111.pth --output .local/matchlook-data/canoncgt-ssl-new-run
```

Output phải mới; không overwrite ảnh đã duyệt. Dữ liệu PST50: zrgong / SA-LUT authors, CC-BY-4.0, provenance và upstream card trong thư mục subset; originals không sửa, thumbnails tạo để xem. Chưa có kiểm thử chân dung đại diện hoặc face model.
