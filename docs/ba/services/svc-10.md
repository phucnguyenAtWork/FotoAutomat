# SVC-10 · Cô lập dữ liệu và ghi audit để bảo vệ từng khách hàng

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-02.

## Công việc và vấn đề cần giải quyết

* Kiểm tra tenant/role ở server cho mọi tài nguyên và thao tác.
* Ghi audit có actor, target, lý do, correlation và lọc thông tin nhạy cảm.

## Màn hình sử dụng

* [BTH-12 · Vận hành cục bộ và xử lý phiên gián đoạn](../screens/bth-12.md)
* [CUS-01 · Đăng nhập, đăng ký và khôi phục tài khoản](../screens/cus-01.md)
* [CUS-02 · Thiết lập tổ chức khách hàng](../screens/cus-02.md)
* [CUS-03 · Tổng quan tài khoản và booth](../screens/cus-03.md)
* [CUS-04 · Danh sách booth](../screens/cus-04.md)
* [CUS-11 · Đơn hàng, thanh toán và chứng từ](../screens/cus-11.md)
* [CUS-17 · Thành viên và phân quyền](../screens/cus-17.md)
* [CUS-18 · Hỗ trợ và yêu cầu xử lý](../screens/cus-18.md)
* [OPS-01 · Tổng quan đội booth toàn hệ thống](../screens/ops-01.md)
* [OPS-02 · Hồ sơ khách hàng thương mại](../screens/ops-02.md)
* [OPS-03 · Quản lý quyền sử dụng và ngoại lệ thương mại](../screens/ops-03.md)
* [OPS-04 · Danh mục phiên bản và bộ cài](../screens/ops-04.md)
* [OPS-06 · Sự cố và lịch sử lệnh từ xa](../screens/ops-06.md)
* [OPS-07 · Đối soát phí dịch vụ và mức sử dụng](../screens/ops-07.md)
* [OPS-08 · Nhật ký kiểm toán và truy cập nội bộ](../screens/ops-08.md)

## Tiêu chí nghiệm thu

* **SVC-10-AC-01:** Thay ID tenant/resource trong request không vượt quyền.
* **SVC-10-AC-02:** Thu hồi quyền có hiệu lực với thao tác nhạy cảm của phiên cũ.
* **SVC-10-AC-03:** Audit không chứa token, activation secret hoặc ảnh khách nguyên bản.

## Quyết định còn mở

[D-05](../decisions.md#d-05), [D-10](../decisions.md#d-10).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
