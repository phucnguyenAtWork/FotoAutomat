# Quy tắc UX dùng chung

Trạng thái: đề xuất để dùng nhất quán trong mọi BA page.

## Booth cảm ứng

* Ưu tiên một hành động chính rõ ràng trên mỗi bước; mục tiêu thao tác đủ lớn và không phụ thuộc hover.
* Hiện bước hiện tại, lựa chọn đã chốt và số tiền trước khi nhận tiền.
* Giữ trạng thái khi retry hoặc quay lại. Chặn double tap tạo giao dịch/lệnh trùng.
* Không cho khách vào cài đặt, xem stack trace hay ảnh người trước.
* Khi có lỗi sau nhận tiền, thông báo quyền lợi còn được giữ và cách gọi hỗ trợ; không tự đưa về màn hình chào mất phiên.
* Timer, số ảnh, số bản và thời hạn màn hình phải lấy từ chính sách duyệt, không tự đặt trong mockup.

## Portal và Ops

* Hiện tenant, bộ lọc và thời điểm dữ liệu cập nhật. Bảng lớn có phân trang.
* Phân biệt thông tin máy chủ mong muốn và booth báo đã áp dụng.
* Review đối tượng, tác động và thời điểm hiệu lực trước thay đổi license, billing hoặc rollout.
* Hiện trạng thái request/server accepted, delivered, running và completed riêng khi nghiệp vụ cần.
* Không sử dụng màu làm tín hiệu duy nhất cho trạng thái; có nhãn chữ và keyboard focus.

## Web nhận ảnh

* Thiết kế ưu tiên điện thoại; tải preview nhỏ trước ảnh gốc.
* Hiển thị chờ upload, sẵn sàng, hết hạn và bị thu hồi khác nhau.
* Không buộc người chụp có tài khoản khách hàng thương mại nếu chính sách giao file không yêu cầu.

## Ma trận trạng thái bắt buộc xem xét

| Trạng thái | Hành vi chung |
| --- | --- |
| Loading | Phản hồi ngay, tránh thao tác trùng; không giả số liệu |
| Empty | Giải thích chưa có dữ liệu và CTA đúng quyền |
| Validation | Chỉ rõ trường sai; giữ dữ liệu đã nhập |
| Permission denied | Không lộ tài nguyên ngoài quyền; có đường trở lại |
| Offline | Booth tiếp tục công việc local được phép; web không giả xác nhận mới |
| Stale | Hiện last seen/last updated; không trình bày như realtime |
| Partial failure | Hiện kết quả từng phần; không báo tất cả thành công |
| Pending external result | Chờ đối soát, không khuyên trả tiền/in lại ngay |
| Retry | Giữ operation ID khi cần idempotency; giới hạn số/tần suất theo thiết kế |
| Completed | Chỉ báo hoàn thành khi có bằng chứng tương ứng nghiệp vụ |

Mỗi BA page ghi ngoại lệ đặc thù. Trạng thái không áp dụng phải được ghi rõ khi làm thiết kế chi tiết, không thêm spinner hoặc modal một cách máy móc.

## Ngôn ngữ

Nội dung BA dùng tiếng Việt. UI copy là bản nháp cần review. Dùng câu cụ thể như “Booth chưa nhận yêu cầu; đang chờ kết nối” thay “Thành công” khi mới xếp lệnh. Giá, thời hạn, quota và SLA lấy từ cấu hình đã duyệt.

## Bàn giao thiết kế

Với mỗi Story, bàn giao wireframe luồng chính, các trạng thái lỗi liên quan, nội dung CTA, quy tắc enable/disable, navigation, phạm vi quyền và mapping tới AC của BA. Bản thiết kế cuối cần liên kết tới Jira khi có file Figma; bộ tài liệu này chưa tạo Figma.
