# SVC-08 · Xử lý AI và đo usage để cung cấp dịch vụ có phí minh bạch

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-05.

## Công việc và vấn đề cần giải quyết

* Quản lý AI jobs, quota và kết quả recipe có thể chạy local.
* Phân biệt reserved/consumed/refunded theo chính sách được chọn.

## Màn hình sử dụng

* [BTH-08 · Làm đẹp sau chụp](../screens/bth-08.md)
* [CUS-10 · Dịch vụ trả phí và mức sử dụng](../screens/cus-10.md)
* [CUS-12 · Thư viện công thức màu](../screens/cus-12.md)
* [CUS-13 · Tạo màu bằng AI và tinh chỉnh](../screens/cus-13.md)
* [OPS-07 · Đối soát phí dịch vụ và mức sử dụng](../screens/ops-07.md)

## Tiêu chí nghiệm thu

* **SVC-08-AC-01:** Retry cùng yêu cầu không tính phí hai lần.
* **SVC-08-AC-02:** Kết quả recipe không tương thích local renderer không được publish.
* **SVC-08-AC-03:** Job lỗi/hủy có cách quyết toán usage đã duyệt và test được.

## Quyết định còn mở

[D-09](../decisions.md#d-09), [D-11](../decisions.md#d-11), [D-12](../decisions.md#d-12).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
