# Kế hoạch train và kiểm chứng AI Match Look

> Cập nhật quyết định 05/10/2026: người dùng chọn CanonCGT làm nền phát triển Match Look và thư viện Digital Camera Looks. Xem [kế hoạch dữ liệu](data-readiness-2026-10-05.md). Đây là quyết định hướng phát triển, chưa phải model đã tích hợp hoặc benchmark đạt.

Ngày 05/10/2026. Đây là **kế hoạch kỹ thuật đề xuất** dựa trên [nghiên cứu](research-2026-10-05.md), chưa phải training code hoặc kết quả đã đạt. Đã có baseline C# và UI Adjust; chưa có model AI Match Look chạy trong ứng dụng.

## Ba công việc khác nhau

**Inference** dùng weights có sẵn để thử ảnh mới. **Fine-tune** cập nhật weights trên dữ liệu booth để thích ứng miền camera/ánh sáng. **Train từ đầu** cần dữ liệu và tài nguyên nhiều hơn; chỉ làm khi thử nghiệm chứng minh cần. Người quản trị thêm một ảnh reference thường là inference, không phải train lại toàn model. Một số phương pháp như D-LUT có bước tối ưu/train riêng cho style, nên phải đo riêng độ trễ tạo look.

## Dữ liệu cần gom

Người dùng đã xác nhận có cả dữ liệu paired và ảnh reference riêng lẻ. Tổ chức như sau:

| Nhóm | Lưu gì | Cách dùng |
| --- | --- | --- |
| A — cùng ảnh trước/sau | Ảnh gốc, bản chỉnh mục tiêu, recipe/XMP/CUBE nếu có, phiên bản renderer | Supervision và benchmark pixel/region; hai ảnh phải được căn đúng để dùng pixel loss |
| B — reference khác cảnh | Ảnh reference, look ID/family do quản trị gán, provenance; có thể chưa biết thông số | Inference benchmark mù; style supervision khi look ID đáng tin. Không tự gán reference làm ground truth của source |
| C — nhiều ảnh cùng phiên | Tập ảnh liên tiếp, camera/picture style/WB/ánh sáng, cùng look mục tiêu | Kiểm tra consistency; tách Normalize từng ảnh và look dùng chung |
| D — calibration camera | RAW + JPEG nếu có, chart/gray card, nhiều mức sáng và WB, picture style/version | Profile đầu vào theo camera; không trộn việc mô phỏng tone camera khác với profile hiệu chuẩn |

Manifest mẫu: [dataset.example.json](dataset.example.json). Đây là dữ liệu minh họa, không có ảnh thật và **không đủ điều kiện train**. Khi thực hiện cần importer/validator kiểm tra file, checksum, quyền sử dụng, split và quan hệ asset. Chưa có importer được triển khai trong lượt này.

Mỗi asset có ID, đường dẫn tương đối, SHA256, ảnh cha/capture ID, session ID, subject-group ID ẩn danh, camera, profile/picture style, ánh sáng, nguồn và quyền train. Không dùng tên khách làm label. Reference và target được kiểm tra quyền độc lập; được gửi ảnh để thử màu không tự đồng nghĩa với được dùng train sản phẩm.

## Pilot đề xuất

Để ra quyết định đầu tiên, đề xuất **300 ảnh gốc độc lập**, khoảng **20 look** do quản trị có quyền sử dụng/tạo, thêm **100 tình huống reference khác cảnh** và **20 nhóm mỗi nhóm 5 ảnh** để xét consistency. Đây là quy mô pilot để phát hiện lỗi, không phải lượng dữ liệu đủ train hoàn chỉnh hay bảo đảm >90%.

300 ảnh × 20 look cho tối đa 6.000 biến thể có nhãn, nhưng vẫn chỉ có 300 nội dung gốc. Không coi số biến thể là số cảnh độc lập. Bắt đầu với camera đang có, bổ sung X-H2/R50/R100 khi có dữ liệu thực tế, không sinh metadata giả cho đủ bảng.

Thu thập đủ biến thiên: ánh sáng ấm/lạnh/hỗn hợp, flash, high ISO, makeup, nhiều mức màu da, tóc/nền/áo gần màu nhau, highlight sát ngưỡng, thiếu sáng và nhiều khuôn mặt. Chưa cần train nhận dạng danh tính; phân nhóm chủ thể chỉ nhằm kiểm soát rò rỉ và đánh giá.

## Chia tập trước khi tạo biến thể

1. Chia nhóm capture/session/subject ở mức ảnh gốc; sau đó mới tạo crop, resize và ảnh áp preset. Toàn bộ biến thể của cùng ảnh theo cùng split.
2. Gợi ý train/validation/test 70/15/15 theo nhóm, không theo từng file. Nếu ràng buộc nhóm giao nhau, gộp thành thành phần liên thông để tránh một người/burst xuất hiện ở hai split.
3. Lập test “camera chưa học”, “look-family chưa học” riêng. Không dùng camera/kiểu look trong train rồi gọi đó là unseen-camera/unseen-look.
4. Source, reference, canonical và ground truth trong một example phải thuộc cùng split dữ liệu nội bộ. Dataset benchmark công khai dành cho đánh giá, không trộn vào train.
5. Kiểm tra SHA256 và near-duplicate; không dò test để chọn hyperparameter. Cặp ảnh đã dùng nhiều lần trong phát triển như DSCF9358/test.jpg là **development examples**, không phải test độc lập.

## Tạo example học reference khác cảnh

Chọn hai ảnh khác nội dung `A`, `B` trong cùng split và một look có quyền `L`:

```text
input     = ảnh A với bias camera/Exposure/WB có kiểm soát
reference = Render(B, L)
target    = Render(A chuẩn hóa, L)
```

Mục tiêu dạy model lấy **tone** từ B, không chép nội dung B. Cặp input–target dùng để học có thể cùng cảnh; điều đó không bắt người dùng nhập hai ảnh cùng cảnh lúc sử dụng. Với ảnh chỉnh tay thật, target phải là bản quản trị đã chấp nhận của chính source. Khi không có canonical ground truth, dùng nhánh loss tương ứng, không giả một ảnh đẹp là “màu trung tính đúng”.

Cần giới hạn synthetic bias theo renderer đã định nghĩa. Nhân sáng trên JPEG không phải đo đúng thay đổi exposure ở cảm biến; mất highlight không thể đảo lại bằng nhãn. Tránh augment làm clipping nghiêm trọng rồi ép model khôi phục dữ liệu không tồn tại.

## Train theo ba mốc có đầu ra rõ ràng

### Mốc 1 — chạy baseline và pretrained

Chốt manifest/split, lưu commit SHA, model SHA256, phiên bản thư viện, preprocessing và seed. Chạy v2/v3 và CanonCGT trước; tiếp đến PETAL/D-LUT khi điều kiện thành phần được kiểm tra. Không cài dependency model vào WPF runtime giai đoạn này. Lưu ảnh, lỗi, runtime p50/p95, peak RAM/VRAM, điều kiện phần cứng. Bảng [experiments.json](experiments.json) hiện để `not_run`.

Repo CanonCGT công bố demo với các đối số `--pretrained_path`, `--inp_path`, `--ref_path`, `--out_path`. Luồng train chính thức gồm grade extractor, canonicalizer, styler, end-to-end và SSL; phải chỉnh dataset loader/config cho dữ liệu riêng, không chỉ đổi một đường dẫn. Các lệnh dưới là **lệnh upstream để tham khảo sau khi setup**, chưa chạy trong repo FotoAutomat:

```bash
python demo.py --gpu 0 --pretrained_path ./pretrained/SSL.pth --inp_path input.png --ref_path reference.png --out_path output.png
python main.py --gpu 0 --yaml Stage1_style_encoder --run_mode train
```

Xem [hướng dẫn tác giả](https://github.com/Jinwon-Ko/CanonCGT#-train). Pin commit và đọc config thực tế thay vì chép hyperparameter từ tóm tắt paper. Ví dụ [config stage 1](https://github.com/Jinwon-Ko/CanonCGT/blob/main/configs/Stage1_style_encoder.yaml) dùng AdamW, batch 64 và LUT grid 17; đó không phải cấu hình đã xác minh phù hợp máy booth.

### Mốc 2 — fine-tune theo booth

Chỉ bắt đầu khi baseline cho thấy model có triển vọng nhưng còn lỗi miền dữ liệu:

- Học embedding: cùng look trên khác nội dung phải gần nhau; khác look trên cùng nội dung phải phân biệt được. Đánh giá không chỉ trên preset đã thấy.
- Học chuẩn hóa: input có bias → canonical mục tiêu; tách label Exposure/WB khỏi thông số look nếu có nhãn đáng tin.
- Học chuyển look: input + reference → target. Loss ảnh chỉ giữa output và target cùng hình học; vùng quan trọng như da/trung tính cần mask/nhãn phù hợp, không xem face ellipse là skin segmentation.
- Thêm regularization LUT mượt, tránh clipping/banding và loss consistency nhóm. Không buộc mọi ảnh cùng brightness vì có cảnh cố ý sáng/tối khác nhau.
- Fine-tune phần output/adapter trước, rồi cân nhắc mở encoder. Dùng early stopping theo validation, lưu best/last checkpoint và logs. So với pretrained gốc và v3, không chỉ nhìn training loss giảm.

Đây là thiết kế thí nghiệm của FotoAutomat. Hệ số loss, learning rate, số epoch và batch size phải được chọn qua validation/profiling; chưa có giá trị đã tối ưu.

### Mốc 3 — xuất recipe và chạy trong app

Hai nhánh cần so:

**A. AI dự đoán recipe trực tiếp:** neural network xuất các tham số có bounds; renderer có thể vi phân trong training phải khớp `ColorProcessor`. Cần kiểm chứng thứ tự WB/curve/HSL, color space, rounding và 8-bit của runtime trước khi học. Không dùng nhãn XMP như thể thuật toán Lightroom và FotoAutomat giống hệt.

**B. AI xuất biến đổi/LUT, rồi fit recipe:** so output AI với `ColorProcessor` trên ảnh và color probes; dùng engine paired nội bộ hoặc optimizer mới để tìm recipe. Báo sai số fit trên mẫu giữ riêng. Khi recipe không đủ biểu diễn, giữ LUT riêng; không cam kết mọi slider phản ánh đúng toàn bộ LUT. Nếu thêm recipe + residual LUT, phải định nghĩa thứ tự chính xác và tái kiểm chứng, không lấy phép trừ LUT tùy ý.

Ưu tiên thử B để có đối chiếu chất lượng sớm, rồi đo xem A có đáng đầu tư để giảm thời gian fit và tăng khả năng chỉnh tay. Quyết định cuối cùng dựa trên độ giống, kiểm soát tham số, tính ổn định và latency.

## Protocol đánh giá

| Protocol | Đo gì | Không được kết luận |
| --- | --- | --- |
| Cùng source có target thật | Delta E theo vùng, PSNR/SSIM, clipping, giữ gradient/chi tiết | Không suy ra model nhận đúng thông số gốc |
| Reference khác cảnh không có target | Quản trị đánh giá mù, màu da/trung tính, ý đồ tone, thời gian chỉnh tay | Không dùng pixel RMSE/PSNR trực tiếp giữa source và reference |
| Cùng phiên / khác camera | Độ biến động look và Normalize, lỗi theo từng nhóm camera/ánh sáng | Một chart hoặc một ảnh đạt không có nghĩa toàn thư viện đạt |
| Python → ONNX → C# | Sai khác output, trục LUT, màu biên, CPU/GPU, hủy/chạy lại | Export ONNX thành công không chứng minh runtime Windows đúng |

Tách hai bảng điểm: **AI chưa chỉnh tay** và **AI + quản trị trong thời gian cố định**. Trước test khóa cần chốt với người dùng “>90%” là độ giống theo đánh giá hay tỷ lệ trường hợp chấp nhận; không tự chọn cách định nghĩa có lợi. Luôn báo số trường hợp, phân bố lỗi và khoảng tin cậy; các reviewer không biết tên model.

## Phần cứng và chi phí cần đo

Máy train và máy booth là hai vai trò. Ngày 05/10/2026 người dùng cho biết khả năng cao dùng máy văn phòng nhỏ, còn xem xét phần cứng; chưa xác nhận GPU/VRAM/RAM. Vì vậy không lấy GPU rời làm yêu cầu mặc định của booth. Trước khi mua hoặc thuê máy train: chạy smoke test 1 batch, ghi peak VRAM rồi đo một epoch để dự toán. Không chạy training dài, thuê GPU hoặc upload ảnh khách trong đợt nghiên cứu tài liệu.

Ưu tiên đo hai đường chạy trên CPU: (1) phân tích reference khi quản trị tạo preset, có tiến trình và hủy; (2) áp recipe/LUT đã lưu cho ảnh khách. Cache look theo reference và phiên bản model để tránh phân tích lại. Normalize thích ứng mỗi ảnh vẫn cần được đo riêng; LUT cố định không tự chuẩn hóa Exposure/WB. Nếu model đầy đủ quá nặng, thử model nhỏ/distillation hoặc tạo thư viện preset trên máy riêng, ghi rõ giới hạn khi booth không có AI thích ứng. Chưa cam kết thời gian tức thì, dung lượng RAM hoặc hỗ trợ model nào trước benchmark phần cứng thật.

Paper CanonCGT báo full training khoảng 7 ngày trên một RTX 3090; đây là số tác giả báo, không phải dự toán của FotoAutomat hoặc cam kết với dữ liệu riêng. [Phụ lục paper](https://arxiv.org/html/2606.01638v1). Giảm batch/resolution hoặc chỉ fine-tune có thể thay đổi tài nguyên/chất lượng; cần đo. Không suy từ VRAM inference ra VRAM training.

## Tiêu chí hoàn thành từng bước

1. **Data-ready:** manifest hợp lệ, provenance rõ, split chống rò rỉ, preprocessing tái lập được.
2. **Benchmark-ready:** baseline + ít nhất một AI chạy cùng ảnh, có output thực và thất bại được lưu, không chỉ kết quả paper.
3. **Fine-tune-ready:** có lỗi miền dữ liệu cần sửa, training loader và loss đã kiểm tra bằng overfit tập nhỏ, tài nguyên đo được.
4. **App-ready:** output có thể chỉnh/lưu, parity runtime đạt ngưỡng chốt trước, có hủy/fallback/phiên bản; xử lý offline.
5. **Release-ready:** test camera/ánh sáng giữ riêng đạt tiêu chí người dùng chấp thuận, provenance model hoàn chỉnh và pilot booth thật.

Không dùng việc có giao diện AI, loss giảm hoặc unit test pass để tuyên bố đã đạt chất lượng Match Look.
