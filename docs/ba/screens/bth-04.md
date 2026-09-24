# BTH-04 · Chọn gói chụp và màu

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-04, chưa phải Jira issue key |
| Mục tiêu | Chọn sản phẩm, số bản in và công thức màu trước khi xác nhận giá phiên. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn chọn sản phẩm, số bản in và công thức màu trước khi xác nhận giá phiên.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-03](../screens/bth-03.md), [BTH-05](../screens/bth-05.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 1 • Chọn trải nghiệm
Gói A [giá / số ảnh / bản in]  Gói B [...]
Màu [preview mẫu] [preview mẫu] [preview mẫu]
Tóm tắt gói • màu • số tiền phải trả
[Quay lại] [Tiếp tục thanh toán]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Danh sách gói và giá|Bảng giá local đã áp dụng|Tiền tệ và nội dung gói hiển thị rõ; không có giá mặc định do dev tự đặt |
| Công thức màu|Phiên bản đã tải và publish|Chỉ hiện recipe tương thích renderer tại booth |
| Ảnh mẫu|Asset minh họa đã duyệt|Ghi rõ là minh họa, không phải ảnh khách |
| Tóm tắt lựa chọn|Phiên hiện tại|Giữ khi quay lại trước thanh toán |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Chọn gói/màu|Lựa chọn tương thích với booth|Cập nhật tóm tắt và số tiền |
| Tiếp tục thanh toán|Đã chọn đầy đủ|Chốt snapshot giá và nội dung cho phiên, mở BTH-05 |

## Luồng chính

1. Khách xem các gói đang bán.
2. Chọn gói và màu.
3. Xem số ảnh, bản in và giá tổng.
4. Xác nhận sang thanh toán.

## Quy tắc nghiệp vụ đề xuất

* **BTH-04-BR-01:** Luồng gói/màu trước thanh toán là đề xuất cần duyệt D-04.
* **BTH-04-BR-02:** Sau khi nhận tiền, thay đổi lựa chọn ảnh hưởng giá cần chính sách riêng; không âm thầm tính lại.
* **BTH-04-BR-03:** Cấu hình mới từ server không đổi snapshot phiên đang phục vụ.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Không có gói/màu dùng được|Quay về trạng thái tạm ngừng có hỗ trợ |
| Nội dung đang tải|Không hiển thị như đã sẵn sàng |
| Offline|Dùng danh mục đã áp dụng; không cho chọn recipe chỉ có trên cloud |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-04-AC-01:** Given recipe mới chưa tải xong; When mở danh sách; Then recipe đó không được chọn cho phiên.
* **BTH-04-AC-02:** Given giá cấu hình thay đổi sau khi đã sang thanh toán; When nhận tiền; Then phiên vẫn dùng snapshot giá đã xác nhận.
* **BTH-04-AC-03:** Given chưa chọn gói; When bấm tiếp tục; Then UI chỉ rõ lựa chọn còn thiếu.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-05](../services/svc-05.md), [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-04](../decisions.md#d-04), [D-08](../decisions.md#d-08), [D-09](../decisions.md#d-09).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
