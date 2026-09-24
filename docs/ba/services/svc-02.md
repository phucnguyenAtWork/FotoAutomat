# SVC-02 · Quản lý quyền sử dụng để hỗ trợ thuê, vĩnh viễn và offline

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-04.

## Công việc và vấn đề cần giải quyết

* Tách entitlement desktop, seat, cập nhật và dịch vụ cloud.
* Phát hành quyền local có thể xác minh và xử lý gia hạn/thu hồi theo policy.

## Màn hình sử dụng

* [BTH-01 · Cài đặt và kích hoạt booth](../screens/bth-01.md)
* [BTH-03 · Màn hình chào và trạng thái phục vụ](../screens/bth-03.md)
* [CUS-06 · Tải phần mềm và cấp mã kích hoạt](../screens/cus-06.md)
* [CUS-07 · So sánh gói thuê, mua vĩnh viễn và dịch vụ](../screens/cus-07.md)
* [CUS-08 · Checkout và kết quả thanh toán dịch vụ](../screens/cus-08.md)
* [CUS-09 · Giấy phép và thuê bao](../screens/cus-09.md)
* [OPS-02 · Hồ sơ khách hàng thương mại](../screens/ops-02.md)
* [OPS-03 · Quản lý quyền sử dụng và ngoại lệ thương mại](../screens/ops-03.md)

## Tiêu chí nghiệm thu

* **SVC-02-AC-01:** Hết hạn cloud không tự hủy quyền desktop vĩnh viễn ngoài điều khoản đã duyệt.
* **SVC-02-AC-02:** Phiên đã trả tiền được xử lý theo chính sách gián đoạn đã thống nhất.
* **SVC-02-AC-03:** Quy tắc offline, thay đổi đồng hồ và thời hạn xác minh có kịch bản kiểm thử.

## Quyết định còn mở

[D-01](../decisions.md#d-01), [D-02](../decisions.md#d-02), [D-03](../decisions.md#d-03).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
