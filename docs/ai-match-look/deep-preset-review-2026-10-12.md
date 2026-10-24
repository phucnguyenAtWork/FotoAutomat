# Deep Preset: đánh giá cho FotoAutomat

Ngày đọc nguồn: 12/10/2026. Phạm vi: nghiên cứu, chưa tải weights, chưa tích hợp hoặc train Deep Preset.

Nguồn chính: [paper](https://arxiv.org/pdf/2007.10701), [trang tác giả](https://minhmanho.github.io/deep_preset/), [repo tác giả](https://github.com/minhmanho/deep_preset).

## Kết luận

Phù hợp để nghiên cứu cách học phong cách màu độc lập với nội dung và dự đoán recipe. Chưa chọn làm engine thay CanonCGT: code/weights được tác giả ghi chỉ dành cho nghiên cứu và phi thương mại. Không đưa các thành phần đó vào sản phẩm thương mại khi chưa có quyền phù hợp. Đọc phương pháp không đồng nghĩa đã được cấp quyền cho code, weights, ảnh hoặc presets.

## Điều đáng học từ bài

Paper dùng dữ liệu nhiều nội dung cùng preset, nhánh dự đoán 69 thông số và Positive Pair-wise Loss (PPL) để kéo gần embedding của cùng phong cách. Đây là các ý tưởng gần với mục tiêu Match Look; không phải nhận dạng danh tính khuôn mặt. Bài có đánh giá chân dung nhưng không chứng minh hệ thống booth sẽ giữ da tốt trên mọi khách.

README nêu PPL có lợi cho nhánh sinh ảnh nhưng làm nhánh dự đoán preset kém hơn trong thực nghiệm của tác giả. Vì vậy phải đánh giá recipe và ảnh riêng, không coi thêm PPL là chắc chắn tốt hơn. Kiến trúc sinh ảnh là encoder–decoder; không phải đầu ra LUT trực tiếp. README cũng cảnh báo training code công bố chưa chạy được ngay và chưa kiểm tra lại.

## Đối chiếu với pilot hiện tại

Pilot FotoAutomat đã có source/reference khác nội dung, cùng recipe tổng hợp; chưa có PPL hay supervised head dự đoán thông số. Target chỉ biểu diễn phép chỉnh tăng thêm trên JPEG, không xác định preset gốc của ảnh khách đã chỉnh màu. 8 recipe hiện tại chưa đủ làm đại diện cho thư viện look thực tế.

Chỉ head `Restyler.generate_lut.outc` được cập nhật. Embedding trong `CanonAdapter.prepare` tạo dưới no_grad, và `PreparedStore` cache đặc trưng backbone cố định. Thêm loss trên embedding đóng băng sẽ không huấn luyện được embedding đó. Nếu thử PPL, phải thiết kế nhánh trainable và thay đổi chiến lược cache; không chèn loss vào pipeline hiện tại rồi gọi đó là đã áp dụng Deep Preset.

Lần train vừa rồi giảm MAE nhưng low-key và lỗi miền RGB tệ hơn. Chưa có bằng chứng Deep Preset khắc phục các lỗi này; phải kiểm tra riêng dữ liệu, objective và khả năng biểu diễn.

## Thí nghiệm đề xuất, chưa thực thi

1. Tạo bộ dữ liệu nguồn có provenance rõ và recipe do mình có quyền sử dụng. Với ảnh sạch X, ảnh khác nội dung A và B, cùng recipe p: target R(X,p), references R(A,p), R(B,p). Giữ renderer/version, tham số p và input profile. JPEG camera trực tiếp vẫn mang picture style; không mặc định là màu trung tính vật lý.
2. Dựng phép đo cố định X,p và đổi A/B: so chênh lệch đầu ra, recipe hoặc LUT ở cùng các màu kiểm tra; đo riêng da, highlights, neutral, monochrome, gamut và low-key. Đánh giá riêng look đã thấy và look chưa thấy; giữ cách ly nội dung/session và test.
3. So sánh baseline với nhánh có supervision recipe, sau đó thử thêm consistency/PPL trên biểu diễn có thể học. Giữ dữ liệu, số bước và seed tương đương; không đổi nhiều yếu tố rồi gán nguyên nhân cải thiện cho một loss.
4. Nếu mục tiêu triển khai là LUT trên CPU, ưu tiên predictor recipe/LUT và renderer xác định. Nhánh sinh ảnh phụ thuộc ngữ cảnh có thể không tái tạo chính xác bằng một LUT toàn cục; phải đo sai số khi chuyển đổi. Thông số Lightroom dự đoán không tự cho cùng kết quả trong renderer của app.

Đây là kế hoạch nghiên cứu độc lập dựa trên phương pháp, chưa là cam kết triển khai hoặc cho phép thương mại. Chưa có benchmark CPU/ONNX Deep Preset trên NUC. Không diễn giải PSNR, LPIPS hoặc tỷ lệ được người dùng chọn thành phần trăm giống màu reference.
