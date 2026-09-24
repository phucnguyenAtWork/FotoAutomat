# CUS-18 · Hỗ trợ và yêu cầu xử lý

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Thành viên tổ chức được cấp quyền |
| Epic tham chiếu | EP-03 |
| Mã màn hình | CUS-18, chưa phải Jira issue key |
| Mục tiêu | Gửi và theo dõi yêu cầu liên quan booth, phiên hoặc giao dịch dịch vụ. |

## Câu chuyện người dùng

Là thành viên tổ chức được cấp quyền, tôi muốn gửi và theo dõi yêu cầu liên quan booth, phiên hoặc giao dịch dịch vụ.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-05](../screens/cus-05.md), [CUS-11](../screens/cus-11.md), [CUS-16](../screens/cus-16.md), [OPS-06](../screens/ops-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Hỗ trợ | [Tạo yêu cầu]
Loại vấn đề • booth/order/session liên quan
Mô tả • diagnostic đã che thông tin nhạy cảm
Lịch sử trao đổi • trạng thái
[Gửi yêu cầu] [Bổ sung thông tin]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Đối tượng liên quan|Booth/order/session thuộc tenant|Không cho gắn tài nguyên tenant khác |
| Mô tả/đính kèm|Người dùng nhập|Không khuyến khích gửi ảnh khách hoặc secret |
| Diagnostic package|Dữ liệu chọn lọc|Cho xem nội dung trước khi gửi |
| Trạng thái yêu cầu|Support service|Có timestamps, không tự hứa SLA |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Gửi yêu cầu|Có mô tả và quyền|Tạo request ID duy nhất |
| Gửi diagnostic|Người dùng chủ động xác nhận nội dung|Upload dữ liệu đã lọc theo policy |

## Luồng chính

1. Chọn đối tượng cần trợ giúp.
2. Mô tả và chọn dữ liệu gửi.
3. Xác nhận gửi.
4. Theo dõi phản hồi và trạng thái.

## Quy tắc nghiệp vụ đề xuất

* **CUS-18-BR-01:** Support ticket không tự cho phép nhân viên truy cập ảnh/điều khiển máy.
* **CUS-18-BR-02:** Kênh support và SLA còn D-05/D-12.
* **CUS-18-BR-03:** Tích hợp hệ thống hỗ trợ không mặc nhiên là Jira nội bộ của đội phát triển.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Upload thất bại|Giữ nội dung form và cho gửi lại an toàn |
| Booth offline|Vẫn tạo yêu cầu; diagnostic remote ở trạng thái pending |
| Không đủ quyền xem session|Chỉ cho gửi mã hỗ trợ phù hợp |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-18-AC-01:** Given cùng request được gửi lại; When retry; Then không tạo yêu cầu trùng ngoài ý muốn.
* **CUS-18-AC-02:** Given diagnostic chứa dữ liệu cấm theo policy; When upload; Then bị loại/chặn và thông báo.
* **CUS-18-AC-03:** Given ticket đã tạo; When support xem; Then quyền tài nguyên vẫn được kiểm tra riêng.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-10](../decisions.md#d-10), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
