# SVC-09 · Điều phối thiết bị và phục hồi phiên để tránh mất tiền hoặc in trùng

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-01.

## Công việc và vấn đề cần giải quyết

* Tích hợp adapter thật sau hardware proof; lưu intent và bằng chứng phiên.
* Điều phối camera, bộ nhận tiền, xử lý ảnh và printer bằng trạng thái bền vững.

## Màn hình sử dụng

* [BTH-02 · Cấu hình và kiểm tra thiết bị](../screens/bth-02.md)
* [BTH-03 · Màn hình chào và trạng thái phục vụ](../screens/bth-03.md)
* [BTH-04 · Chọn gói chụp và màu](../screens/bth-04.md)
* [BTH-05 · Thanh toán tại booth](../screens/bth-05.md)
* [BTH-06 · Live view và chụp ảnh](../screens/bth-06.md)
* [BTH-07 · Xem và chọn ảnh](../screens/bth-07.md)
* [BTH-08 · Làm đẹp sau chụp](../screens/bth-08.md)
* [BTH-09 · Chọn khung và xác nhận bản in](../screens/bth-09.md)
* [BTH-10 · Tiến trình in và lỗi in](../screens/bth-10.md)
* [BTH-12 · Vận hành cục bộ và xử lý phiên gián đoạn](../screens/bth-12.md)
* [CUS-16 · Phiên chụp, doanh thu booth và giao file](../screens/cus-16.md)
* [OPS-06 · Sự cố và lịch sử lệnh từ xa](../screens/ops-06.md)

## Tiêu chí nghiệm thu

* **SVC-09-AC-01:** Mất điện ở các bước nhận tiền/chụp/in có quy trình khôi phục kiểm chứng.
* **SVC-09-AC-02:** Print unknown không tự gửi lại.
* **SVC-09-AC-03:** Luồng local được kiểm thử không Internet trên cấu hình booth đại diện.

## Quyết định còn mở

[D-04](../decisions.md#d-04), [D-07](../decisions.md#d-07), [D-08](../decisions.md#d-08), [D-11](../decisions.md#d-11).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
