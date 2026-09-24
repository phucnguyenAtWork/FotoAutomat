# SVC-07 · Xếp hàng giao file để tiếp tục phục vụ khi mất mạng

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-07.

## Công việc và vấn đề cần giải quyết

* Lưu công việc upload bền vững, retry có kiểm soát và cấp quyền download.
* Áp dụng retention, thu hồi link và kiểm tra quyền trên từng file.

## Màn hình sử dụng

* [BTH-11 · Nhận file qua QR và kết thúc](../screens/bth-11.md)
* [CUS-16 · Phiên chụp, doanh thu booth và giao file](../screens/cus-16.md)
* [DL-01 · Mở QR và kiểm tra tình trạng giao file](../screens/dl-01.md)
* [DL-02 · Bộ ảnh và tải xuống trên điện thoại](../screens/dl-02.md)

## Tiêu chí nghiệm thu

* **SVC-07-AC-01:** Restart không làm mất công việc upload chưa hoàn tất.
* **SVC-07-AC-02:** Chỉ báo file sẵn sàng sau khi publish thành công.
* **SVC-07-AC-03:** Token không cho tải ảnh tenant hoặc phiên khác; hết hạn được kiểm tra phía server.

## Quyết định còn mở

[D-06](../decisions.md#d-06), [D-10](../decisions.md#d-10).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
