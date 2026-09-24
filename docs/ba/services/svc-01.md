# SVC-01 · Đăng ký thiết bị để ràng buộc booth với tổ chức và seat

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-02.

## Công việc và vấn đề cần giải quyết

* Cấp danh tính thiết bị và mã kích hoạt có thời hạn.
* Xử lý request lặp, chuyển/thu hồi seat và đối soát trạng thái đăng ký.

## Màn hình sử dụng

* [BTH-01 · Cài đặt và kích hoạt booth](../screens/bth-01.md)
* [CUS-06 · Tải phần mềm và cấp mã kích hoạt](../screens/cus-06.md)
* [CUS-09 · Giấy phép và thuê bao](../screens/cus-09.md)
* [OPS-02 · Hồ sơ khách hàng thương mại](../screens/ops-02.md)

## Tiêu chí nghiệm thu

* **SVC-01-AC-01:** Kích hoạt lặp cùng request không tạo thêm booth hoặc chiếm thêm seat.
* **SVC-01-AC-02:** Thiết bị không thể tự đổi tenant bằng dữ liệu phía client.
* **SVC-01-AC-03:** Chuyển seat giữ lịch sử thiết bị cũ và tuân thủ policy đã duyệt.

## Quyết định còn mở

[D-02](../decisions.md#d-02), [D-05](../decisions.md#d-05).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
