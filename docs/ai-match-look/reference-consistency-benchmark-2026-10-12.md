# Benchmark đổi reference cùng recipe

## Phạm vi

Đây là benchmark phát triển, không phải blind test hay huấn luyện. Dùng validation của pack `finetune-prep-2026-10-12-002`, không dùng train/test để tạo ca. So sánh SSL chính thức với `finetune-2026-10-12-001/last.pth`; LAST chưa được duyệt chất lượng và không tự triển khai vào WPF.

12 source được lấy cách đều từ danh sách ID validation đã sắp xếp. Pool reference loại toàn bộ 12 source; mỗi source dùng 4 reference khác ID, chọn cách đều trong pool còn lại. Mỗi nhóm giữ source, target, recipe và danh sách reference cố định cho cả hai model. 8 recipe × 12 source × 4 reference = 384 ca/model; mỗi nhóm có 6 cặp đầu ra để so sánh. Manifest và SHA-256 checkpoint được lưu cùng kết quả.

Việc khác ID không chứng minh khác danh tính hoặc nội dung hoàn toàn độc lập. Tập validation hiện tại thuộc một session, không đủ chứng minh tổng quát hóa cảnh quan hoặc camera khác. Không suy ra hiệu quả trên mọi khuôn mặt từ bộ này.

## Metric

- **Pairwise MAE /255:** trung bình chênh lệch giữa 6 cặp đầu ra trong nhóm. Đo ảnh RGB đã clamp như ảnh được hiển thị. Có thêm worst pair của mỗi nhóm; khi tổng hợp là trung bình worst pair, không phải cực đại toàn tập.
- **Target MAE /255:** sai số với target sRGB tổng hợp biết trước, báo cùng độ nhất quán để tránh thưởng model bỏ qua reference.
- **Skin:** đo trên mask da dự đoán của source; không phải mask ground truth. Không có hỗ trợ vùng da thì trả null, không coi là sai số 0.
- **Highlights:** mask cố định từ target, trung bình RGB >= 0.9; đây là định nghĩa thao tác cho benchmark, không phải nhận diện clipping vật lý.
- **RGB/neutral/BW gate:** dùng cùng policy hiện có, đo RGB thô trước clamp. Vi phạm đếm theo ca–tiêu chí; không đồng nghĩa số ảnh lỗi.
- **Repeat control:** chạy lại ca đầu tiên mỗi model, báo max absolute difference. Chỉ kiểm tra lặp lại ca đó, không chứng minh mọi ca bất biến với mọi môi trường.

Benchmark không dự đoán lại thông số preset gốc. Target là phép chỉnh tăng thêm trên JPEG vốn có màu camera/preset. Thấp hơn ở pairwise chưa chứng minh đẹp hoặc đúng màu; chưa định nghĩa ngưỡng nghiệm thu consistency và không tự chọn checkpoint theo benchmark này.

## Tái hiện

Chạy từ thư mục FotoAutomat, dùng thư mục output mới:

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-ReferenceConsistency.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Benchmark-ReferenceConsistency.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --output .local\matchlook-data\reference-consistency-2026-10-12-001
```

Kết quả: `plan.json`, `summary.json`, JSON từng model chứa từng ca/nhóm/look, `index.html` và ảnh PNG. Trang HTML minh họa một source với 8 look và 4 reference, không phải toàn bộ 12 source.

## Trạng thái

Đã chạy xong trong 178,4 giây trên CPU hiện tại. Ba kiểm thử đã qua: dự đoán cố định có độ biến thiên 0; metric vùng da tách được thay đổi ở nền; kế hoạch chỉ lấy validation và source/reference pool không giao nhau. Không cập nhật weights; hash hai checkpoint không đổi. Repeat control có chênh lệch 0 ở ca được lặp của cả hai model.

| Metric /255, thấp hơn tốt hơn | SSL gốc | Fine-tune LAST |
|---|---:|---:|
| Biến thiên khi đổi reference | 16,713 | 8,567 |
| Biến thiên vùng da | 17,000 | 10,194 |
| Sai số target | 21,972 | 18,077 |
| Sai số target vùng da | 23,559 | 18,744 |
| Sai số target highlights | 16,276 | 12,898 |

Mask da có hỗ trợ ở 96/96 nhóm, highlights ở 84/96 nhóm; các trung bình tương ứng dùng đúng số nhóm có hỗ trợ. Mỗi nhóm có 4 ca, hỗ trợ vùng được lấy từ target/source cố định.

Low-key bị suy giảm: biến thiên reference 6,731 → 11,814; target MAE 22,979 → 46,498. Film-matte nhất quán hơn (23,474 → 6,921) nhưng target sai hơn (18,430 → 25,061). Đây là ví dụ trực tiếp vì sao không thể chọn model chỉ theo consistency.

Cả hai không đạt quality gate. SSL có 200 vi phạm ca–tiêu chí; LAST có 418: tỷ lệ kênh vượt miền 149, mức vượt miền 173, neutral 48 và monochrome 48. Không diễn giải số này là 418 ảnh lỗi.

Kết luận phát triển: LAST cải thiện một số metric tổng hợp nhưng không đủ để thay SSL/đưa vào sản phẩm. Ưu tiên chẩn đoán low-key và film-matte, kiểm tra tín hiệu style/normalization trong dữ liệu và loss. Benchmark này chưa xác định nguyên nhân nhân quả hoặc chứng minh cần/đủ PPL; phải có ablation trước khi kết luận. Chưa chạy thêm fine-tune.
