# SVC-05 · Đồng bộ nội dung có phiên bản để booth dùng dữ liệu nhất quán

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-05.

## Công việc và vấn đề cần giải quyết

* Tạo manifest cho recipe, frame và bảng giá; xác minh tài nguyên trước áp dụng.
* Theo dõi desired/reported và giữ phiên bản cũ khi tải thất bại.

## Màn hình sử dụng

* [BTH-04 · Chọn gói chụp và màu](../screens/bth-04.md)
* [BTH-09 · Chọn khung và xác nhận bản in](../screens/bth-09.md)
* [CUS-05 · Chi tiết booth và tác vụ từ xa](../screens/cus-05.md)
* [CUS-12 · Thư viện công thức màu](../screens/cus-12.md)
* [CUS-13 · Tạo màu bằng AI và tinh chỉnh](../screens/cus-13.md)
* [CUS-14 · Thư viện và chỉnh sửa khung ảnh](../screens/cus-14.md)
* [CUS-15 · Phát hành nội dung tới booth](../screens/cus-15.md)

## Tiêu chí nghiệm thu

* **SVC-05-AC-01:** Thiếu asset hoặc checksum sai không thay active bundle.
* **SVC-05-AC-02:** Phiên đang chạy giữ snapshot nội dung và giá.
* **SVC-05-AC-03:** Booth offline không bị báo đã áp dụng bundle mới.

## Quyết định còn mở

[D-09](../decisions.md#d-09), [D-13](../decisions.md#d-13).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
