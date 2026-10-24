# Kiểm tra 24 asset ứng viên holdout — 13/10/2026

## Kết quả: không đủ điều kiện never-used holdout

Manifest có 48 validation asset. Plan 128 ca của loss ablation dùng 24 asset khác nhau (source hoặc reference); 24 asset còn lại là ứng viên được đề xuất. **Cả 24 ứng viên đều đã tham gia validation thực chạy ở cả 5 epoch pilot fine-tune.** Không khóa holdout mới hoặc thay đổi split.

| Epoch | Case có metric thực chạy | Source khác nhau | Reference khác nhau | Ứng viên đã dùng |
|---|---:|---:|---:|---:|
| 1 | 384 | 48 | 48 | 24/24 |
| 2 | 384 | 48 | 48 | 24/24 |
| 3 | 384 | 48 | 48 | 24/24 |
| 4 | 384 | 48 | 48 | 24/24 |
| 5 | 384 | 48 | 48 | 24/24 |

Đây là sử dụng để đánh giá, không khẳng định optimizer đã train trên ảnh validation. Nhưng các ảnh đã ảnh hưởng quá trình đánh giá/chọn hướng phát triển, nên không còn độc lập theo yêu cầu “chưa xuất hiện trong bất kỳ run nào”.

## Bằng chứng và phương pháp

Script `tools/MatchLookData/Audit-ValidationExposure.py`:

1. Đọc manifest hiện tại và bản snapshot trong run fine-tune `finetune-2026-10-12-001`.
2. Xác định tập ứng viên bằng hiệu giữa 48 validation ID và union source/reference trong experiment 128 ca.
3. Join **case ID có metric trong history.jsonl từng epoch** với example trong manifest snapshot để xác định source/reference thực dùng. Không chỉ dựa vào ảnh có tên trong manifest hoặc plan chưa chạy.
4. Kiểm tra file specification của asset giữa hai manifest, rồi tính SHA256 của 48 PNG validation để xác nhận đúng nội dung ảnh.
5. Lưu từng asset, hash ảnh, epoch, case ID, vai trò source/reference và metric làm execution witness.

Manifest hiện tại và snapshot giống byte, SHA256:
`710f3e9c14a87fa3be4ca9aeb394b3e42dfa1ae3d14e4faa0fae1891bab01b00`.

History SHA256:
`6c1d9f42a93ea9f4629e0ad0cefa16545657a9d256e6705ecf06930f7f7bdc4d`.

Plan 128 ca SHA256:
`cf8b19d7df0e60377576a1c3c96283d2ca00c82173a4e67ea901289fae87dd4a`.

48/48 hash ảnh validation khớp manifest. Các hash đầu vào được kiểm tra lại sau audit. Artifact:

- `.local/matchlook-data/validation-exposure-2026-10-13-001/summary.json`
- `.local/matchlook-data/validation-exposure-2026-10-13-001/asset-witnesses.json`

Một run thực chạy đã đủ bằng chứng bác bỏ điều kiện never-used cho mọi ứng viên; không cần quét tất cả run cũ để chứng minh lại điều đã xác nhận. Ngược lại, nếu không tìm thấy witness, audit này sẽ trả requires_further_history_audit, không tự gán chưa dùng.

## Hướng tiếp tục

- Có thể chạy đối chứng fixed-gain-before-correction trên dữ liệu hiện tại và chọn α ở đây, nhưng phải gọi là **dev tuning**. Không cần dừng nghiên cứu vì chưa có holdout mới.
- Giữ 30 asset test trong manifest cho nghiệm thu cuối. Audit này không đọc pixel test, không chạy inference trên test, không di chuyển ảnh hoặc đổi split. Điều đó không thay thế một audit toàn bộ lịch sử để chứng nhận test chưa từng bị dùng ở mọi run cũ.
- Nếu cần một lần kiểm chứng độc lập cho α trước nghiệm thu cuối, thu thập nhóm ảnh mới độc lập theo phiên/subject, xác minh trùng lặp/provenance, khóa trước khi chọn α. Chia lại 24 ảnh đã dùng chỉ tạo dev subset, không tạo holdout mới.
- Không quét hoặc chọn α bằng điểm holdout. Nếu tiếp tục chỉnh sau khi xem holdout thì phải ghi nhận tập đó đã trở thành dữ liệu phát triển.

## Tái hiện

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Audit-ValidationExposure.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --run .local\matchlook-data\finetune-2026-10-12-001 --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\validation-exposure-2026-10-13-001
```

Chọn output mới khi chạy lại. Đây là audit metadata/hash/metric, không suy luận model hoặc fine-tune. Các assertion ID, split, duplicate case, asset specification và file hash đã qua trên dữ liệu thực.
