# OPS-07 · Đối soát phí dịch vụ và mức sử dụng

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Nhân sự tài chính/billing có quyền |
| Epic tham chiếu | EP-04 |
| Mã màn hình | OPS-07, chưa phải Jira issue key |
| Mục tiêu | Giải quyết lệch trạng thái giữa thanh toán, quyền sử dụng và usage của khách hàng. |

## Câu chuyện người dùng

Là nhân sự tài chính/billing có quyền, tôi muốn giải quyết lệch trạng thái giữa thanh toán, quyền sử dụng và usage của khách hàng.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-02](../screens/ops-02.md), [OPS-03](../screens/ops-03.md), [OPS-08](../screens/ops-08.md), [CUS-11](../screens/cus-11.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Đối soát thương mại
Order | provider status | entitlement | usage
Bộ lọc lệch trạng thái • kỳ tính phí
Timeline events • credit/refund đề xuất
[Đối soát] [Ghi điều chỉnh có căn cứ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Payment events|Billing provider đã xác minh|Lưu event identity; không tin payload browser |
| Usage ledger|Service events|Đơn vị, kỳ và trạng thái quyết toán |
| Điều chỉnh|Quyết định có quyền|Bắt buộc lý do và audit; không sửa lịch sử gốc |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Đối soát order|Có quyền|Tra cứu provider và hệ thống nội bộ |
| Điều chỉnh/hoàn|Theo policy và thẩm quyền|Tạo giao dịch điều chỉnh, chờ xác nhận kết quả |

## Luồng chính

1. Lọc giao dịch lệch.
2. Đọc các nguồn bằng chứng.
3. Chọn biện pháp theo policy.
4. Theo dõi đến khi quyền và ledger nhất quán.

## Quy tắc nghiệp vụ đề xuất

* **OPS-07-BR-01:** Giao dịch adjustment có định danh để không lặp tác dụng.
* **OPS-07-BR-02:** Sự kiện đến sai thứ tự không được đưa order từ paid về pending vô căn cứ.
* **OPS-07-BR-03:** Không tự quyết chính sách thuế, refund hoặc giá theo suy luận kỹ thuật.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Provider chưa phản hồi|Giữ pending reconciliation |
| Usage trùng|Đánh dấu và không thu trùng |
| Không đủ thẩm quyền refund|Chuyển người phê duyệt |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-07-AC-01:** Given webhook thanh toán lặp; When đối soát; Then không tạo doanh thu dịch vụ trùng.
* **OPS-07-AC-02:** Given điều chỉnh đã gửi nhưng timeout; When thao tác lại; Then dùng cùng định danh để không hoàn hai lần.
* **OPS-07-AC-03:** Given staff support thường; When yêu cầu refund; Then bị từ chối.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-03](../services/svc-03.md), [SVC-08](../services/svc-08.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-09](../decisions.md#d-09), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
