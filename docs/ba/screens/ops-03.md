# OPS-03 · Quản lý quyền sử dụng và ngoại lệ thương mại

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Staff được quyền quản lý license |
| Epic tham chiếu | EP-04 |
| Mã màn hình | OPS-03, chưa phải Jira issue key |
| Mục tiêu | Cấp, điều chỉnh hoặc thu hồi quyền theo quyết định thương mại có thể truy vết. |

## Câu chuyện người dùng

Là staff được quyền quản lý license, tôi muốn cấp, điều chỉnh hoặc thu hồi quyền theo quyết định thương mại có thể truy vết.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-02](../screens/ops-02.md), [OPS-08](../screens/ops-08.md), [CUS-09](../screens/cus-09.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Quyền sử dụng | tenant • sản phẩm
Desktop | seat | update eligibility | cloud
Thay đổi dự kiến • thời điểm hiệu lực • lý do
Booth bị ảnh hưởng • phiên đang chạy
[Review] [Xác nhận theo thẩm quyền]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Entitlement hiện tại|License service|Hiện nguồn cấp và hiệu lực |
| Thay đổi|Người có quyền đề xuất|Bắt buộc lý do và phạm vi |
| Booth chịu tác động|Device bindings|Phân biệt server updated với booth acknowledged |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Cấp/gia hạn/thu hồi|Có permission và policy|Review tác động rồi tạo thay đổi có audit |
| Ngoại lệ offline|Theo thẩm quyền D-03|Quyền có phạm vi/thời hạn xác định, không hardcode vĩnh viễn |

## Luồng chính

1. Chọn đúng tenant và entitlement.
2. Xem trạng thái và căn cứ.
3. Nhập thay đổi, lý do và hiệu lực.
4. Xác nhận theo phân quyền và theo dõi booth nhận.

## Quy tắc nghiệp vụ đề xuất

* **OPS-03-BR-01:** Không sửa license bằng chỉnh trực tiếp dữ liệu trên máy khách.
* **OPS-03-BR-02:** Không tự biến hết hạn thuê bao cloud thành thu hồi bản desktop đã mua.
* **OPS-03-BR-03:** Quy trình duyệt hai người nếu cần là D-05, chưa mặc định tồn tại.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Thay đổi trùng/đồng thời|Yêu cầu đọc lại revision |
| Booth offline|Hiện pending acknowledgement |
| Ngoại lệ không đủ thẩm quyền|Chặn và nêu quyền cần thiết |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-03-AC-01:** Given staff không có quyền thu hồi; When gửi lệnh; Then bị từ chối và không đổi entitlement.
* **OPS-03-AC-02:** Given ngoại lệ được cấp; When xem audit; Then có actor, reason, hiệu lực và giá trị trước/sau.
* **OPS-03-AC-03:** Given booth chưa nhận thay đổi; When xem kết quả; Then không hiện đã áp dụng tại thiết bị.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-02](../services/svc-02.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-02](../decisions.md#d-02), [D-03](../decisions.md#d-03), [D-05](../decisions.md#d-05).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
