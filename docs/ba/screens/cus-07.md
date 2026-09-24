# CUS-07 · So sánh gói thuê, mua vĩnh viễn và dịch vụ

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Khách tiềm năng / chủ tổ chức |
| Epic tham chiếu | EP-04 |
| Mã màn hình | CUS-07, chưa phải Jira issue key |
| Mục tiêu | Hiểu chính xác sản phẩm mua một lần và dịch vụ có phí định kỳ trước khi đặt hàng. |

## Câu chuyện người dùng

Là khách tiềm năng / chủ tổ chức, tôi muốn hiểu chính xác sản phẩm mua một lần và dịch vụ có phí định kỳ trước khi đặt hàng.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-01](../screens/cus-01.md), [CUS-08](../screens/cus-08.md), [CUS-09](../screens/cus-09.md), [CUS-10](../screens/cus-10.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Chọn cách sử dụng FotoAutomat
Thuê [ ] | Mua vĩnh viễn [ ]
Bảng: quyền desktop • seat • cập nhật • hỗ trợ
Dịch vụ thêm: AI • lưu ảnh/QR • quản lý fleet
[Chọn gói] • Điều kiện và phí được giải thích
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Giá/tiền tệ/chu kỳ|Catalog thương mại đã duyệt|Không tự đặt giá hay nhãn miễn phí |
| Quyền desktop|Entitlement catalog|Nêu rõ giới hạn và phiên bản được dùng |
| Dịch vụ cloud|Service catalog|Nêu quota, cách tính phí và khi hết hạn |
| Thông tin thuế/phí|Chính sách thị trường được chọn|Chưa chốt thì đánh dấu cần xác nhận |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Chọn gói|Sản phẩm đang bán và có giá hợp lệ|Tạo lựa chọn checkout |
| So sánh quyền|Luôn có|Hiện khác biệt thuê/vĩnh viễn/cloud rõ ràng |

## Luồng chính

1. Xem hai mô hình sở hữu.
2. So sánh quyền desktop và quyền dịch vụ.
3. Chọn số seat và dịch vụ được cung cấp.
4. Xem tóm tắt rồi sang checkout.

## Quy tắc nghiệp vụ đề xuất

* **CUS-07-BR-01:** Mua vĩnh viễn không được tự mô tả là bao gồm cloud vô hạn nếu chưa được duyệt.
* **CUS-07-BR-02:** Quyền cập nhật và quyền dùng bản đã mua là hai thuộc tính riêng.
* **CUS-07-BR-03:** Giá công bố phải khớp checkout tại thời điểm xác nhận quote.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Chưa có catalog duyệt|Hiện liên hệ hoặc bản nháp nội bộ, không cho mua giá giả |
| Sản phẩm không bán tại thị trường|Giải thích và không cho checkout |
| Giá thay đổi|Yêu cầu xem lại quote trước thanh toán |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-07-AC-01:** Given gói vĩnh viễn chỉ gồm desktop; When xem so sánh; Then cloud và cập nhật trả phí được ghi riêng.
* **CUS-07-AC-02:** Given quote hết hiệu lực; When tiến hành mua; Then phải xác nhận giá mới.
* **CUS-07-AC-03:** Given không có giá sản phẩm hợp lệ; When bấm mua; Then không tạo giao dịch số tiền mặc định.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-02](../services/svc-02.md), [SVC-03](../services/svc-03.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
