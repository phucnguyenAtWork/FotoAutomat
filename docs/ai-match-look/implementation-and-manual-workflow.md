# CanonCGT: quy trình triển khai và thao tác thủ công

Ngày 05/10/2026. Đây là kế hoạch thực hiện, không phải báo cáo model đã chạy. Hai nhóm chính: **chân dung có mặt** và **cảnh quan/cảnh cụ thể**; ảnh có cả người và cảnh phải được đánh giá cả hai. CanonCGT đã được chọn làm nền. Model mặt chưa được chọn; không có số đo độ giống trên dataset của FotoAutomat.

## 1. Model hoạt động thế nào?

Theo [paper CanonCGT](https://arxiv.org/html/2606.01638v1), grade extractor dùng MobileNet-v2 tạo vector tone; canonicalizer đưa ảnh về miền trung gian, grader áp tone reference. Hai bước màu dự đoán LUT 3D thích ứng ảnh. DP-CGT gồm supervised learning từ preset rồi self-supervised refinement. Trong supervised stage, contrastive learning kéo ảnh cùng preset gần nhau trong không gian tone. Đây không phải model nhận dạng danh tính hoặc đọc ngược thông số camera.

Thiết kế bổ sung của FotoAutomat: source/reference → chuẩn hóa kỹ thuật → CanonCGT → kiểm tra vùng người/cảnh → draft trong Adjust → duyệt/lưu. Nhánh face detection + landmarks + skin segmentation hoạt động riêng; trước hết dùng đánh giá, chỉ thêm bảo vệ da cục bộ khi có dữ liệu chứng minh cần. Không gọi face boxes là skin mask. Không cần biết người trong ảnh là ai.

## 2. Thứ tự thực hiện kỹ thuật

| Bước | Công việc sẽ làm | Đầu ra / điều kiện chuyển bước |
| --- | --- | --- |
| 1. Chốt bộ kiểm thử | Chia portrait, scene, mixed; look mục tiêu, camera, ánh sáng; khóa rubric và tập test | Danh sách trường hợp và tiêu chí đạt, không đổi sau khi thấy kết quả |
| 2. Inventory và quyền | Nguồn ảnh, quyền train/evaluation/public preview, checksum, metadata | Manifest; ảnh chưa rõ quyền ở quarantine |
| 3. Data preprocessing | Decode, orientation, ICC, màu làm việc, chất lượng, duplicate, nhãn và split | Originals bất biến + working copies tái tạo được + báo cáo lỗi |
| 4. Load weights | Pin commit, lấy SSL/E2E chính thức, hash, kiểm tra config/tensor keys | Model nạp đủ weights, không bỏ qua missing/unexpected keys âm thầm |
| 5. Inference baseline | Chạy từng checkpoint trên cùng source/reference; identity case, ảnh khác tỷ lệ, nhiều mặt | Output, lỗi, thời gian CPU và RAM; chưa train |
| 6. Nhánh khuôn mặt | Benchmark detector/landmarks/parser có license phù hợp; kiểm tra nhãn thủ công | Recall mặt, false detections, lỗi landmarks/mask theo nhóm |
| 7. Benchmark hai miền | So baseline hiện tại với CanonCGT nguyên bản; đánh giá người/cảnh/mixed | Bảng điểm và danh sách lỗi, có cả thất bại |
| 8. Fine-tune có kiểm soát | Chỉ triển khai sau khi xác định lỗi; thử từng thay đổi trên validation | Model candidate tốt hơn, không dùng test để chọn checkpoint |
| 9. Recipe và tối ưu CPU | Fit recipe hoặc giữ LUT; kiểm tra output Python/C#; cache, threads, INT8 nếu phù hợp | Báo cáo sai số chuyển đổi, latency, RAM, sai khác chất lượng |
| 10. Pilot Adjust | Reference → preview → Impact → chỉnh tay → Save; thumbnail cùng source | Thử trên máy booth thật, nhiều ảnh cùng phiên, rollback được |

Không bắt buộc hoàn thành train từ đầu trước bước 5. GPU train nằm ngoài booth. Nếu inference CPU quá chậm phải ghi số đo, rồi thử giảm chi phí/model nhỏ; không tự giảm chất lượng âm thầm.

## 3. Data preprocessing cụ thể

### Giữ và chuẩn hóa

- Giữ original RAW/JPEG, hash, parent capture/session, camera/style/firmware nếu biết, renderer/version, ICC, WB, exposure và quyền. Unknown để null, không suy đoán nhãn chắc chắn từ tên file.
- Sửa hướng bằng EXIF; chuyển profile bằng color management có kiểm chứng. RAW cần renderer cố định. Ảnh log/HDR phải đi nhánh chuyển đổi rõ ràng, không đọc như JPEG sRGB.
- Tạo bản phân tích theo preprocessing checkpoint đã pin; bản xuất giữ độ phân giải gốc. Resize/mask/landmarks phải cùng hệ tọa độ, lưu phép biến đổi.
- SHA256 tìm trùng byte; hash thị giác chỉ gợi ý near-duplicates. Giữ các variant tone liên quan trong một nhóm, không xóa nhãn huấn luyện hữu ích.
- Chia train/validation/test theo nhóm capture/session/subject trước augmentation; không đưa crop của ảnh test vào train. Giữ thêm camera/ánh sáng chưa thấy để kiểm tra khả năng tổng quát hóa.

### Cờ chất lượng

Đo kích thước, blur, clipping theo kênh, compression, độ sáng/phân bố màu; đánh dấu screenshot/watermark, mặt quá nhỏ, che khuất và mixed light. Cờ tự động không quyết định thay con người. Ảnh hỏng loại, ảnh khó giữ thành nhóm riêng. Không lọc hết da tối, high ISO hoặc ảnh thiếu sáng để làm điểm số đẹp hơn.

Không auto-WB/histogram-equalize toàn bộ reference trước model: có thể xóa tone mục tiêu. Không lấy màu da reference làm màu tuyệt đối bắt mọi người theo. Không ép reference và source cùng cảnh/crop; chỉ source và target dùng pixel supervision mới phải căn đúng.

GPS, serial máy và tên người không cần làm input. ISO/khẩu/tốc độ giữ để phân tích lỗi, không bắt buộc đầu vào neural network. Quyền ảnh và nguồn style là dữ liệu bắt buộc dù không vào model.

## 4. Training methods

**Tái lập upstream trước:** chạy pretrained; nếu cần tái lập training thì theo chuỗi grade extractor → canonicalizer/grader → end-to-end → self-supervised của [repo chính thức](https://github.com/Jinwon-Ko/CanonCGT). Không coi dữ liệu/preset upstream mặc nhiên được dùng thương mại; xem source registry.

**Fine-tune đề xuất cho FotoAutomat:**

1. Tạo triplet: source A, reference B có look L, target A do quản trị render với look L. A và B khác nội dung; target A phải cùng hình học source A. Target là ý đồ chỉnh được duyệt, không phải khẳng định chỉ có một màu đúng.
2. Dùng preset/recipe mình có quyền để tạo supervision. Chọn render cố định, không coi số XMP là cùng thuật toán với C#.
3. Thử freeze extractor và fine-tune canonicalizer/grader trước; so với cập nhật toàn bộ bằng validation. Đây là thí nghiệm đề xuất, không phải quy tắc upstream đã chứng minh cho booth.
4. Giữ mục tiêu tái tạo màu/cấu trúc và regularization phù hợp upstream. Thử bổ sung loss vùng da so với target, vùng trung tính và consistency nhóm; cân trọng số bằng validation. Không phạt da khác màu source nếu đó là tone có chủ ý; không ép mọi màu da về một giá trị.
5. Với unpaired, tái lập self-supervised upstream trên data được phép trước khi mở rộng. Không dùng hai ảnh khác cảnh làm pixel target.
6. Trộn portrait/scene theo batch có kiểm soát, báo riêng từng miền; overfit tập nhỏ để kiểm tra loader/loss trước training dài. Chọn epoch, learning rate, batch và freeze policy sau đo tài nguyên, không đưa con số giả chính xác.
7. Ablation: model gốc → fine-tune → thêm loss da → thêm kiểm soát vùng; thay từng yếu tố để biết phần nào cải thiện. Giữ test khóa đến cuối.

Model detector/parser dùng riêng và có quy trình giấy phép, validation riêng; chưa cần train đồng thời với CanonCGT. Fine-tune màu không tự làm detector chính xác hơn.

## 5. Chân dung và cảnh quan cần đánh giá gì?

| Miền | Nhãn/vùng cần kiểm tra | Lỗi không được bỏ sót |
| --- | --- | --- |
| Portrait | Tất cả face boxes, landmarks phù hợp schema model, skin mask loại mắt/môi/tóc; cổ/tai nếu mask hỗ trợ | Bỏ sót người, nhầm nền thành da, da đỏ/xám/cháy kênh, viền mask, mặt-cổ lệch |
| Scene | Loại cảnh, ánh sáng, vùng trời/cây/nước/vật trung tính khi có; ROI thủ công trước khi chọn segmentation | Ép cây sang màu da, trời banding, bóng đổ ám màu ngoài ý đồ, mất chi tiết |
| Mixed | Người và cảnh trong cùng ảnh, nhiều cỡ mặt | Nền đẹp nhưng da hỏng, bảo vệ một người bỏ người khác |

Face precision/recall phải so nhãn thủ công với quy tắc ghép box (đề xuất IoU ≥ 0.5 cho benchmark pilot, khóa trước test). Báo cả mặt bỏ sót và false positives. Mask đo IoU/Dice; landmarks đo sai số chuẩn hóa theo quy tắc thống nhất. Báo riêng mặt nhỏ, nghiêng, che, kính, nhiều người; confidence detector không phải xác suất màu đúng.

Không thấy mặt trong cảnh quan là bình thường. Có người nhưng detector lỗi: giữ global output cho review, không bịa mask và tự động publish. Face module ban đầu chỉ đánh giá; local skin protection là tính năng thử nghiệm sau này, cần mask mềm và kiểm tra đường biên ở độ phân giải xuất.

## 6. Đo phần trăm thế nào?

**Hiện tại: chưa có % thực nghiệm của FotoAutomat. Không có một % độ giống chung suy ra từ tên CanonCGT.** Báo PSNR/SSIM/Delta E riêng khi có target căn chỉnh; không lấy SSIM × 100 hoặc 100 − RMSE làm “% giống màu”. Với reference khác cảnh không dùng pixel similarity trực tiếp giữa reference và output.

Đề xuất báo ba loại số:

1. **Tỷ lệ ảnh được chấp nhận:** 100 × số trường hợp đạt rubric / tất cả trường hợp trong test khóa. Timeout, crash và ảnh không có kết quả vẫn tính thất bại, báo riêng lý do. Báo tổng và từng miền/camera/ánh sáng.
2. **Tỷ lệ sau chỉnh tay:** cùng công thức, cho thời gian chỉnh cố định (đề xuất pilot 60 giây/ảnh). Lưu delta recipe và thời gian; không trộn với AI tự động.
3. **Mức sai khác định lượng:** Delta E trên vùng tương ứng, clipping, PSNR/SSIM có target, cùng latency p50/p95 và RAM. Không đổi thành % cảm tính.

Rubric pilot đề xuất: ít nhất 3 reviewer xem mù model, chấm tone theo reference 1–5 và màu vùng người/cảnh 1–5; chấp nhận nếu ít nhất 2/3 reviewer cho cả hai tiêu chí ≥4, không có lỗi cấu trúc hoặc artifact nghiêm trọng sau adjudication. Quy tắc này phải chốt trước test; cảnh có tone phi tự nhiên chủ ý chấm theo ý đồ, không buộc “tự nhiên” tuyệt đối.

Ví dụ **giả định, không phải kết quả model**: test 200 trường hợp, 182 đạt → 91% chấp nhận; không có nghĩa mỗi ảnh giống màu 91%. Báo số lượng, khoảng tin cậy 95% và resample theo capture/session để không coi 20 variant cùng RAW là 20 cảnh độc lập. Mục tiêu >90% chỉ có ý nghĩa sau khi chốt rubric và phạm vi; test nhỏ không bảo đảm toàn bộ camera.

| Chỉ số sẽ báo | Portrait | Scene | Mixed |
| --- | --- | --- | --- |
| Số test độc lập | Chưa thu | Chưa thu | Chưa thu |
| Chấp nhận AI tự động | Chưa đo | Chưa đo | Chưa đo |
| Chấp nhận sau chỉnh tay | Chưa đo | Chưa đo | Chưa đo |
| Face recall / mask quality | Chưa đo | Không áp dụng nếu không có người | Chưa đo |
| Thời gian CPU / RAM | Chưa đo | Chưa đo | Chưa đo |

## 7. Quy trình làm bằng tay cho người chuẩn bị data

1. **Tạo phiên thu ảnh:** đặt session ID, chọn camera/ánh sáng, ghi style/WB/firmware và quyền sử dụng. Tách portrait, scene, mixed nhưng cho phép một ảnh có nhiều nhãn.
2. **Chụp/lưu:** lấy RAW + JPEG nếu có; giữ originals. Chân dung gồm nhiều góc và nhiều người, scene có bóng/sáng và màu đa dạng. Chụp chart/gray card bổ sung, không để chart thành vật model dựa vào ở mọi ảnh.
3. **Chọn reference:** mô tả tone mong muốn bằng từ đơn giản (tối lạnh, da ấm, bóng xanh...). Ghi nguồn; tránh ảnh screenshot/nén nặng làm chuẩn. Không cần người/cảnh trùng source.
4. **Làm target:** mở source trong công cụ chỉnh màu; quản trị tạo bản mục tiêu theo reference, xuất profile rõ ràng và lưu recipe/version. Giữ crop/kích thước cho paired evaluation; không retouch/warp khuôn mặt trong target dùng đo chuyển màu. Nếu có sửa hình học thì tách task.
5. **Duyệt kỹ thuật:** kiểm tra hướng ảnh, màu/profile, mất chi tiết và trùng lặp. Ghi accepted/quarantine/rejected cùng lý do, không xóa originals ngay.
6. **Gán nhãn mặt:** kiểm từng người, sửa box/landmarks/mask sai. Mắt, môi, tóc, trang sức không tính da; ghi che khuất hoặc uncertain thay vì vẽ giả phần không thấy. Kiểm cổ/tai và vùng da nhỏ.
7. **Gán vùng cảnh:** nếu cần, đánh dấu ROI trời/cây/nước/trung tính nhìn thấy; không đoán vật trắng từ màu pixel. Lưu tọa độ theo ảnh đã sửa orientation.
8. **Chia tập:** mọi bản cùng capture/burst và nhóm liên quan ở một split. Đặt test ở danh sách khóa; không xem output test để chỉnh model liên tục.
9. **So kết quả:** xem source, reference, target (nếu có), output; dùng cùng màn hình/profile, tắt chế độ làm ấm màn hình khi review. Xem vừa khung để đánh giá tone và 100% để tìm clipping/banding/viền. Không đánh giá bằng screenshot gửi qua chat.
10. **Ghi lỗi cụ thể:** face missing, skin red, neutral cast, wrong scene hue, banding, slow, wrong metadata; chấm rubric trước khi chỉnh tay. Lưu bản AI riêng, chỉnh bản sao và ghi thời gian.
11. **Duyệt preset:** thử look trên nhiều ảnh/phiên chưa dùng tạo look; mọi thumbnail dùng cùng source. Chỉ publish sau duyệt; label reference-inspired hoặc camera-validated đúng bằng chứng.

## 8. Những điều cần chú ý

- Không có face detection chính xác 100% mặc định; cần fallback và đánh giá nhiều mặt.
- Model thống nhất tone không được đổi danh tính/hình học; không dùng ảnh đã làm đẹp nặng để đánh giá riêng màu.
- Không áp cùng một màu da tuyệt đối cho mọi người hoặc ép trời/cây của cảnh khác thành cùng pixel.
- Adaptive Normalize và look cố định khác nhau: lưu LUT từ một ảnh không bảo đảm cả phiên đã được chuẩn hóa.
- Không tự cho phép ảnh khách dùng train chỉ vì được gửi để thử filter; quyền preview công khai cũng riêng.
- Không bảo đảm “camera-validated” chỉ từ tên máy/EXIF; cần holdout và điều kiện camera đầu vào cụ thể.
- Chưa chọn face model, chưa train, chưa benchmark; tài liệu này là cam kết phạm vi công việc và cách kiểm chứng, không phải kết quả hoàn thành.

Tài liệu liên quan: [data readiness](data-readiness-2026-10-05.md), [training plan](training-plan.md), [source registry](source-registry.json).
