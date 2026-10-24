> Bổ sung sau đợt basic UI: [bộ chỉnh màu với ảnh thật](color-preview.md) đã nối Adjust và ảnh mẫu vào luồng booth. Những mô tả bên dưới về slider màu chưa xử lý ảnh là trạng thái trước đợt này.

# UI/UX Windows — cập nhật 02/10/2026

## Đã triển khai trong đợt này

* Nối luồng khách: chụp → chọn ảnh → làm đẹp → chọn khung/đặt ảnh → in mô phỏng → xem ảnh phiên.
* Chọn ảnh: hiển thị toàn bộ shots; bật/tắt ảnh yêu thích; không cho tiếp tục khi bỏ chọn hết.
* Làm đẹp: thanh kéo cảm ứng 0–100, đặt lại, chọn ảnh đang xem; giữ thông số khi quay lại. Đây là UI và trạng thái phiên, chưa render làm đẹp.
* Khung: quay lại bước làm đẹp không mất các ô đã ghép; sửa danh sách ảnh sẽ xóa các gán ô để tránh dùng ảnh đã bỏ chọn.
* Giao ảnh: gallery ảnh mô phỏng và tổng kết phiên; bỏ QR giả khỏi màn hình này. Chưa có tải tệp hoặc upload.
* Máy ảnh: chọn và lưu model mục tiêu; mở booth mô phỏng. Chưa kết nối SDK hoặc phát hiện thiết bị.
* Bộ lọc màu: tab Adjust/Retouch/Refine, thanh kéo cảm ứng, tên và lưu bản nháp cục bộ. AI Match Look hiện rõ chưa tích hợp. Các thông số chưa áp lên ảnh hoặc phát hành preset.
* Thư viện khung dùng công cụ bố cục/thư viện đã có; Căn chỉnh bản in mở bảng cấu hình in; Quyền quản trị mở bảng PIN hiện có.
* Màn hình & thương hiệu (cập nhật 20/10/2026): tên thương hiệu và logo PNG/JPEG riêng của bên mua key; có khung xem trước. Giao diện khách dùng logo ở header, màn hình chào và chân trang; không có logo thì dùng tên thương hiệu. Màu nhấn, màu nền và ảnh nền theo thương hiệu (một key = một thương hiệu cho bản cài).
* Thông tin ứng dụng: phiên bản và trạng thái tích hợp thực tế.
* Nhật ký: xem hoạt động quản trị của lần mở cửa sổ hiện tại, tối đa 200 mục, có nút làm mới. Chưa phải audit log lưu bền.

## Chưa thể tuyên bố hoàn tất toàn bộ UI/UX

* Preview cần ảnh thật/live view và kết quả AI để kiểm chứng độ khớp với bản in.
* Bộ màu: curves, Color Editor, Color Balance (3-Way), trắng đen, LUT và Match Look đã có (xem [adjust-tools.md](adjust-tools.md), cập nhật 17/10/2026). Còn thiếu version/rollback preset và kiểm thử trên màn cảm ứng thật.
* Còn kích hoạt/license, trạng thái thiết bị và màn hình phục hồi phiên thực tế.
* Các trang quản trị dùng lại bảng hiện có chưa có bố cục riêng cho từng mục.
* QR/tải file tạm gác theo yêu cầu. HTML trong delivery-preview và delivery-editor-preview vẫn là preview độc lập, chưa port toàn bộ sang WPF.
* Thanh toán, camera SDK, AI, spooler máy in và upload chưa được tích hợp. Không nghiệm thu thương mại chỉ dựa vào UI.

## Kiểm tra

83 test tự động đạt; build WPF Release không có warning/error; 4 kịch bản harness mô phỏng đạt. Kiểm tra WPF trên Windows đã tải tất cả mục quản trị và render các template chọn ảnh/làm đẹp/giao ảnh; rà ảnh render của trang màu và làm đẹp. Chưa kiểm thử cảm ứng trên booth thật hoặc thiết bị vật lý.
