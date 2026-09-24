# SVC-04 · Theo dõi heartbeat và lệnh để biết trạng thái thực của booth

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-03.

## Công việc và vấn đề cần giải quyết

* Nhận telemetry theo device identity; lưu thời gian xảy ra và thời gian nhận.
* Quản lý command ID, thời hạn, acknowledgement và kết quả.

## Màn hình sử dụng

* [CUS-03 · Tổng quan tài khoản và booth](../screens/cus-03.md)
* [CUS-04 · Danh sách booth](../screens/cus-04.md)
* [CUS-05 · Chi tiết booth và tác vụ từ xa](../screens/cus-05.md)
* [CUS-15 · Phát hành nội dung tới booth](../screens/cus-15.md)
* [CUS-16 · Phiên chụp, doanh thu booth và giao file](../screens/cus-16.md)
* [CUS-18 · Hỗ trợ và yêu cầu xử lý](../screens/cus-18.md)
* [OPS-01 · Tổng quan đội booth toàn hệ thống](../screens/ops-01.md)
* [OPS-05 · Lập đợt cập nhật và theo dõi rollout](../screens/ops-05.md)
* [OPS-06 · Sự cố và lịch sử lệnh từ xa](../screens/ops-06.md)

## Tiêu chí nghiệm thu

* **SVC-04-AC-01:** Mọi trạng thái sức khỏe đều có lần quan sát gần nhất.
* **SVC-04-AC-02:** Queued, received và succeeded là các trạng thái khác nhau.
* **SVC-04-AC-03:** Lệnh hết hạn không được thực thi muộn; retry không tạo side effect trùng.

## Quyết định còn mở

[D-05](../decisions.md#d-05), [D-13](../decisions.md#d-13), [D-14](../decisions.md#d-14).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
