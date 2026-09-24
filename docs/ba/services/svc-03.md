# SVC-03 · Đối soát thanh toán để cấp quyền và ghi phí chính xác

Trạng thái: yêu cầu nền tảng đề xuất, chưa triển khai thương mại. Epic: EP-04.

## Công việc và vấn đề cần giải quyết

* Xử lý quote, order, provider events, chứng từ và điều chỉnh.
* Liên kết billing với entitlement và metering bằng định danh ổn định.

## Màn hình sử dụng

* [CUS-07 · So sánh gói thuê, mua vĩnh viễn và dịch vụ](../screens/cus-07.md)
* [CUS-08 · Checkout và kết quả thanh toán dịch vụ](../screens/cus-08.md)
* [CUS-09 · Giấy phép và thuê bao](../screens/cus-09.md)
* [CUS-10 · Dịch vụ trả phí và mức sử dụng](../screens/cus-10.md)
* [CUS-11 · Đơn hàng, thanh toán và chứng từ](../screens/cus-11.md)
* [OPS-07 · Đối soát phí dịch vụ và mức sử dụng](../screens/ops-07.md)

## Tiêu chí nghiệm thu

* **SVC-03-AC-01:** Callback lặp hoặc đến sai thứ tự không cấp quyền/ghi phí trùng.
* **SVC-03-AC-02:** Redirect phía browser không tự chuyển đơn thành đã thanh toán.
* **SVC-03-AC-03:** Thu tiền thành công nhưng cấp quyền lỗi có trạng thái và quy trình phục hồi.

## Quyết định còn mở

[D-01](../decisions.md#d-01), [D-12](../decisions.md#d-12).

Đây là năng lực dùng chung; không tạo một backend riêng cho từng màn hình nếu chúng dùng cùng nghiệp vụ. Framework máy chủ, schema chi tiết và API được xác định ở bước thiết kế kỹ thuật.
