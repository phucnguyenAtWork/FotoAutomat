# SVC-06 · Cập nhật booth an toàn để triển khai phiên bản trên toàn fleet

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-06.

## Công việc và vấn đề cần giải quyết

* Xác minh gói phần mềm và metadata tương thích.
* Thực thi khi idle, theo dõi health và khôi phục trong phạm vi schema cho phép.

## Màn hình sử dụng

* [CUS-05 · Chi tiết booth và tác vụ từ xa](../screens/cus-05.md)
* [CUS-06 · Tải phần mềm và cấp mã kích hoạt](../screens/cus-06.md)
* [OPS-04 · Danh mục phiên bản và bộ cài](../screens/ops-04.md)
* [OPS-05 · Lập đợt cập nhật và theo dõi rollout](../screens/ops-05.md)

## Tiêu chí nghiệm thu

* **SVC-06-AC-01:** Gói không hợp lệ không được cài.
* **SVC-06-AC-02:** Không restart giữa phiên đã trả tiền.
* **SVC-06-AC-03:** Rollback bị chặn khi phiên bản cũ không đọc được dữ liệu hiện tại.

## Quyết định còn mở

[D-03](../decisions.md#d-03), [D-11](../decisions.md#d-11), [D-13](../decisions.md#d-13).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
