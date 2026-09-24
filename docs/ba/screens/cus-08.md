# CUS-08 · Checkout và kết quả thanh toán dịch vụ

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / người có quyền billing |
| Epic tham chiếu | EP-04 |
| Mã màn hình | CUS-08, chưa phải Jira issue key |
| Mục tiêu | Thanh toán phí phần mềm và dịch vụ với kết quả có thể đối soát. |

## Câu chuyện người dùng

Là chủ tổ chức / người có quyền billing, tôi muốn thanh toán phí phần mềm và dịch vụ với kết quả có thể đối soát.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-07](../screens/cus-07.md), [CUS-09](../screens/cus-09.md), [CUS-11](../screens/cus-11.md), [CUS-18](../screens/cus-18.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Đơn hàng FotoAutomat
Sản phẩm • seat • chu kỳ • dịch vụ
Tạm tính • phí/thuế nếu áp dụng • tổng
Thông tin chứng từ • phương thức thanh toán
[Xác nhận thanh toán] → trạng thái đơn hàng
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Quote ID và tổng tiền|Billing server|Giá server quyết định; không tin giá browser gửi |
| Phương thức thanh toán|Nhà cung cấp đã chọn|Không tự lưu dữ liệu thẻ nhạy cảm |
| Order status|Sự kiện thanh toán được xác minh|Pending/paid/failed/cancelled/refund-pending tùy policy |
| Quyền được cấp|Entitlement sau thanh toán|Hiện đang cấp nếu chưa hoàn tất |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Thanh toán|Quote hợp lệ và có quyền billing|Tạo một order với idempotency key |
| Kiểm tra trạng thái|Order đang chờ|Đọc server, không tạo giao dịch mới |

## Luồng chính

1. Review đơn hàng và điều kiện.
2. Chọn phương thức thanh toán.
3. Hoàn tất tại provider nếu cần.
4. Trở lại màn hình chờ xác minh rồi xem quyền được cấp.

## Quy tắc nghiệp vụ đề xuất

* **CUS-08-BR-01:** Redirect success từ browser không đủ bằng chứng cấp quyền.
* **CUS-08-BR-02:** Callback/webhook lặp không tạo hai quyền hoặc hai hóa đơn.
* **CUS-08-BR-03:** Đây là giao dịch với FotoAutomat, không phải doanh thu khách chụp tại booth.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Khách đóng trang|Order tiếp tục được đối soát; có thể xem lại |
| Provider timeout|Hiện chưa rõ, không yêu cầu trả lại ngay |
| Đã thu tiền nhưng cấp quyền lỗi|Hiện đang xử lý, tạo incident và cho hỗ trợ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-08-AC-01:** Given browser trả về success nhưng server chưa xác minh; When render; Then trạng thái vẫn chờ xác nhận.
* **CUS-08-AC-02:** Given callback thành công gửi hai lần; When xử lý; Then chỉ một entitlement/order effect được áp dụng.
* **CUS-08-AC-03:** Given đã trả tiền nhưng cấp quyền chưa xong; When mở lại; Then thấy cùng order và trạng thái xử lý, không checkout lại.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-02](../services/svc-02.md), [SVC-03](../services/svc-03.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
