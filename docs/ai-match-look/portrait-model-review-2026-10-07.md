# Portrait model review — 07/10/2026

Đã chạy 96 controlled triplets Phase B qua SSL và E2E trên CPU: 192 outputs, strict-load checkpoint, không fine-tune hay sửa màu hậu kỳ. Chỉ clamp/round để lưu PNG. Input hashes được kiểm tra trước inference; tất cả output finite, cùng shape target và 192 PNG decode hợp lệ. Đã xem overview một source × 8 looks; chưa duyệt thủ công toàn bộ outputs hoặc kiểm thử tương tác HTML.

Người dùng đã nhận xét palette target phù hợp chân dung/tông da. Ghi nhận ở mức palette, không tự điền 96 đánh giá riêng hoặc coi model đạt. Bản ghi nằm trong `target-review-context.json`, gắn hash dataset.

Đầu ra local: `.local/matchlook-data/portrait-model-review-001/index.html`, `overview.jpg`, `report.json`. Trang có Source, Reference, TARGET viền xanh, SSL, E2E; lọc look, nhận xét từng checkpoint và ghi chú da/nền/sáng tối. Xuất JSON để giữ đánh giá; chưa có importer hoặc tự phê duyệt train. Không upload ảnh.

## Kết quả diagnostic

MAE encoded RGB trên thang 0–255, trung bình 12 cases/look; thấp hơn là gần target pixels hơn, không phải phần trăm giống hoặc điểm chất lượng da.

| Look | SSL | E2E |
| --- | ---: | ---: |
| Neutral | 23.76 | 17.75 |
| Warm | 23.01 | 22.45 |
| Cool | 21.82 | 21.21 |
| Pastel | 14.10 | 13.24 |
| Film matte | 20.30 | 15.29 |
| High contrast | 28.48 | 24.18 |
| Low key | 17.90 | 39.94 |
| Monochrome | 32.26 | 26.50 |

Overview cho thấy SSL quá sáng ở một số mặt, E2E chưa theo low-key, cả hai còn màu khi target monochrome. Không kết luận E2E thắng tổng thể từ MAE này. Reference/source đã có preset nền khác nhau nên cùng phép biến đổi bổ sung chưa tạo cùng look tuyệt đối. Target chỉ là mục tiêu kiểm thử có kiểm soát, chưa chứng minh benchmark transfer hợp lệ cho mọi trường hợp.

Chưa có mask mặt/da hoặc metric vùng; chưa khóa holdout, chưa fine-tune. Bước tiếp là ghi lỗi người duyệt và bổ sung target/reference đồng nhất look thực tế, kiểm tra overlap trước split. Không dùng outputs làm ground truth độc lập.

## Tái lập

Từ root FotoAutomat, dùng runtime CPU đã cài:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Review-CanonCgtPortrait.py --dataset .local/matchlook-data/phase-b-pilot-001/dataset.json --repo .local/CanonCGT --weights .local/canoncgt-official-weights --output .local/matchlook-data/portrait-model-review-002
```

Output phải mới, ngoài dataset. Report ghi checkpoint hashes, dataset hash, torch version, CPU/threads, lỗi từng case và thời gian forward (không coi là benchmark NUC).
