# Đối chiếu bốn ưu tiên Match Look — 12/10/2026

## Kết quả lượt này

Đã hoàn tất cơ chế kiểm tra RGB/neutral/BW trước khi chọn best checkpoint trong pipeline nghiên cứu. Chưa hoàn tất hiệu chỉnh ngưỡng và chưa cải thiện màu của model. Không fine-tune CanonCGT; các cập nhật optimizer chỉ diễn ra trong kiểm thử model Linear tổng hợp.

CodeGraph được gọi trước khi đọc code nhưng không tìm thấy các module Python này; đã dùng source trực tiếp, không reindex.

## 1. Tiêu chí chọn checkpoint: đã nối và kiểm thử

- `CheckpointQuality.measure_case`: đo RGB raw trước clamp, tỉ lệ giá trị kênh vượt phạm vi (với tolerance), biên độ vượt phạm vi lớn nhất; neutral đo mean absolute RGB error với source; BW đo trung bình max(R,G,B)-min(R,G,B). Không phải DeltaE hoặc phần trăm giống màu.
- Neutral chỉ áp với nhãn neutral có target gần bằng source; BW chỉ áp với target achromatic. Không ép các creative look khác về da tự nhiên/đen trắng.
- `decide_quality`: kiểm tra từng case, yêu cầu có cả neutral và BW; thiếu nhóm thì không đủ điều kiện. Một case lỗi không được trung bình hóa để che đi.
- `SupervisedLoop.evaluate_validation`: trả per-look MAE, từng case và quyết định chất lượng.
- `TrainingPipeline.fit`: chỉ khi eligible mới xét MAE để ghi best.pth. Case bị loại vẫn ghi last.pth để resume, lý do nằm trong history.jsonl; số epoch không đạt tính vào patience. last.pth không có nghĩa được duyệt chất lượng. Nếu tất cả epoch fail thì không có best.pth. Quy tắc mới và ngưỡng nằm trong fingerprint; checkpoint từ cấu hình khác không được resume âm thầm.

Ngưỡng kỹ thuật dự thảo: range tolerance 1e-6, fraction <=0.001 (0.1% giá trị kênh), excursion <=0.01, neutral MAE <=3/255, BW chroma <=2/255. Đây là mức khởi đầu có chủ ý, CHƯA được hiệu chỉnh bằng mức chấp nhận của người dùng hoặc benchmark độc lập. Không phải chứng nhận chất lượng sản phẩm. Metric neutral/BW dùng raw output; MAE ranking dùng output clamp như trước.

Kiểm thử lượt này: 5 test CheckpointQuality, 3 test SupervisedLoop, 3 test TrainingPipeline đều PASS. Bao gồm raw output >1 có clamped MAE=0 vẫn bị loại; thiếu BW bị loại; neutral/chroma sai bị loại; integration không tạo best.pth cho candidate lỗi, vẫn có last.pth và lý do. Test pipeline hiện có xác minh nạp SSL/adapter giữ nguyên state và synthetic resume.

Áp lại tiêu chí lên 16 ca dry-run, chỉ đánh giá 12 ca có target; 4 ca cross-content không có target không bị gán score giả. Output `.local/matchlook-data/dry-run-quality-2026-10-12-001/report.json`:

- Neutral case01: MAE 21.877/255; case09: 37.386/255, vượt 3/255.
- High contrast case06: 23.907% giá trị kênh raw ngoài [0,1], vượt mức 0.1%; excursion cũng vượt 0.01.
- BW case08: chroma 11.661/255, vượt 2/255.
- Eligibility: false. State SHA256 giữ nguyên `a4ecb944efbc7e74bb9faa6d9af88ba84ee5e413008c6f16f7ee7cba1e8d9fe1`; mọi grad None. 4.844s trong script, ảnh 256px và mask cache. Không phải held-out validation thật, không dùng các ảnh này để chứng minh generalization. Ngưỡng không được nới để SSL pass.

Giúp ích: tránh chọn checkpoint ít sai số trung bình nhưng hỏng neutral/BW hoặc dựa vào clamp để che lỗi. Cải thiện đã chứng minh là hành vi lựa chọn/khả năng phát hiện, KHÔNG phải chất lượng ảnh. Mục 1 hoàn thành phần kỹ thuật nối pipeline; calibration và sửa model vẫn còn.

## 2. Cross-content training: chưa hoàn thành

Đã có 12 ca reference khác session ở cross-reference-001 để đánh giá visual; không có target chuẩn. Bộ 150/26 là allocation ảnh; 51 ảnh train có cảnh báo dHash và chưa được giải quyết hoàn toàn. Structural gradient ranking hỗ trợ rà soát, không phải nhận diện người. Curriculum 792 entries từ 99 ảnh vẫn là same-content; không được gọi là supervised cross-content training.

Vướng mắc: JPEG đã graded và không có ảnh trước chỉnh. Cùng một incremental recipe trên hai base look khác nhau không tạo shared absolute style có đáp án. Không tự ép reference khác cảnh thành target pixel. Bước tiếp theo: thiết kế tự giám sát có kiểm soát và/hoặc lấy cặp trước-sau/corrected targets có căn cứ, giữ quyền dữ liệu và nhóm source/reference trong cùng split. Chưa có bằng chứng cải thiện chuyển look khác cảnh.

## 3. Exposure/WB và style: chưa triển khai phần tách của FotoAutomat

CanonCGT SSL có Canonicalizer và Restyler, adapter đã gọi cả hai. Không đồng nhất canonicalized image với Exposure/WB đã chuẩn hóa vật lý; chưa trích được thông số Exposure/Kelvin/Tint độc lập, chưa tách chúng ra khỏi LUT xuất. Chưa có residual style LUT kèm normalization metadata và kiểm thử khả năng dùng lại. Không báo đã tách chỉ vì upstream có Canonicalizer.

Bước tiếp theo phải định nghĩa contract: normalization theo từng ảnh, style dùng lại, identity/neutral behavior, giới hạn sáng/tint, dữ liệu kiểm thử thay đổi Exposure/WB có đáp án. JPEG thiếu illuminant/RAW khiến một số thông số chỉ là ước lượng. Không cần chỉnh một ảnh mẫu cho đẹp rồi coi là giải pháp tổng quát.

## 4. Nhiều mặt/nền/full resolution: mới có kiểm chứng một phần

Đã có MediaPipe face landmarks + multiclass semantic mask, kiểm tra vùng/độ phủ/IoU qua biến đổi và fail-closed mask; RegionQuality có code. Đã chạy ảnh nhiều người và nền khác nhau 256/512px. Mask cache không xác nhận tất cả mặt được nhận đúng; face localization không phải identity recognition. Nhiều mặt trùng detector và mặt nghiêng đã có lỗi ghi nhận. Threshold chưa calibrated.

Chưa có đủ cảnh quan, full-resolution benchmark, kiểm tra LUT consistency giữa preview/export, kiểm chứng ảnh gốc không mất cấu trúc/texture, benchmark ASUS NUC đích. Global LUT không thể áp cách chỉnh khác nhau cho hai vùng có cùng RGB; loss mask không tạo spatial correction trong inference.

## Trình tự tiếp theo

Mục 1 không cần thêm trang preview để đóng phần nối kỹ thuật. Chuyển trọng tâm sang contract normalization/style và dữ liệu cross-content hợp lệ (mục 2–3), sau đó benchmark mục 4. Không train khi dataset vẫn không có quan hệ target hợp lệ. Báo người dùng trước bất kỳ fine-tune CanonCGT nào. LangSmith/API judge đã dừng theo yêu cầu; không upload ảnh hoặc phát sinh API trả phí.
