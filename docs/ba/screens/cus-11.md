# CUS-11 · Đơn hàng, thanh toán và chứng từ

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / kế toán được cấp quyền |
| Epic tham chiếu | EP-04 |
| Mã màn hình | CUS-11, chưa phải Jira issue key |
| Mục tiêu | Tra cứu lịch sử phí trả cho FotoAutomat và chứng từ tương ứng. |

## Câu chuyện người dùng

Là chủ tổ chức / kế toán được cấp quyền, tôi muốn tra cứu lịch sử phí trả cho FotoAutomat và chứng từ tương ứng.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-08](../screens/cus-08.md), [CUS-10](../screens/cus-10.md), [CUS-18](../screens/cus-18.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Thanh toán của tổ chức
Bộ lọc thời gian • trạng thái • loại sản phẩm
Mã đơn | sản phẩm | số tiền | trạng thái | chứng từ
Chi tiết thanh toán và hoàn tiền nếu có
[Tải chứng từ] [Yêu cầu hỗ trợ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Đơn hàng|Billing ledger|Tiền tệ và số tiền theo giao dịch gốc |
| Chứng từ|Provider/quy trình xuất đã chọn|Không tự gọi là hóa đơn thuế khi chưa phù hợp thị trường |
| Refund status|Sự kiện đã xác minh|Tách yêu cầu hoàn với đã hoàn |
| Thông tin thanh toán|Provider reference|Chỉ dữ liệu đã che theo quyền |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tải chứng từ|Chứng từ có sẵn và thuộc tenant|Trả file được bảo vệ |
| Hỗ trợ giao dịch|Có quyền xem order|Tạo yêu cầu gắn đúng order ID |

## Luồng chính

1. Lọc đơn hàng.
2. Xem chi tiết và các sự kiện.
3. Tải chứng từ khi sẵn sàng.
4. Liên hệ hỗ trợ nếu trạng thái chưa rõ.

## Quy tắc nghiệp vụ đề xuất

* **CUS-11-BR-01:** Không sửa trực tiếp số tiền lịch sử từ UI.
* **CUS-11-BR-02:** Giao dịch booth với khách lẻ được xem ở CUS-16, không cộng vào danh sách này.
* **CUS-11-BR-03:** Quy trình refund và chứng từ theo D-12.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Chứng từ đang tạo|Hiện đang xử lý thay nút tải lỗi |
| Thanh toán chưa rõ|Hiện chờ đối soát |
| Không có đơn|Empty state dẫn đến danh mục dịch vụ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-11-AC-01:** Given order thuộc tenant khác; When truy cập URL; Then không thể tải chứng từ.
* **CUS-11-AC-02:** Given refund mới yêu cầu; When xem trạng thái; Then không hiển thị đã hoàn tiền.
* **CUS-11-AC-03:** Given chứng từ chưa tạo; When mở order; Then UI hiển thị đúng trạng thái chờ.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-03](../services/svc-03.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
