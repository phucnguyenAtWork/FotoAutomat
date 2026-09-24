# BTH-05 · Thanh toán tại booth

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-05, chưa phải Jira issue key |
| Mục tiêu | Nhận tiền cho phiên chụp và cho khách biết chính xác số tiền đã nhận/còn thiếu. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn nhận tiền cho phiên chụp và cho khách biết chính xác số tiền đã nhận/còn thiếu.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-04](../screens/bth-04.md), [BTH-06](../screens/bth-06.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 2 • Thanh toán
Gói đã chọn • Tổng phải trả
Đã nhận [ ]  Còn thiếu [ ]
Mệnh giá chấp nhận • quy tắc tiền thừa
Trạng thái bộ nhận tiền
[Hỗ trợ] • Tự chuyển khi thanh toán đủ
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Giá phiên|Snapshot BTH-04|Chỉ đọc |
| Tiền đã nhận|Sự kiện acceptor đã ghi bền vững|Không tăng theo animation hoặc callback UI |
| Mệnh giá/hỗ trợ trả lại|Khả năng phần cứng + chính sách|Hiển thị trước khi nhận tiền |
| Mã phiên hỗ trợ|Phiên local|Không dùng như token tải ảnh |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Nhận tiền|Phiên hợp lệ, thiết bị đủ sẵn sàng, acceptor được bật|Ghi nhận tiền và cập nhật số còn thiếu |
| Yêu cầu hỗ trợ|Có lỗi hoặc khách cần dừng|Giữ bằng chứng; gọi hướng xử lý thay vì hứa hoàn tiền tự động |

## Luồng chính

1. Hiện gói, giá và điều kiện nhận tiền.
2. Bật acceptor khi đủ điều kiện.
3. Cập nhật từ số tiền thực đã ghi nhận.
4. Khi đủ tiền, khóa việc thu thêm và chuyển sang chụp.

## Quy tắc nghiệp vụ đề xuất

* **BTH-05-BR-01:** Tiền chụp tại booth tách biệt hoàn toàn với phí dịch vụ B2B trên portal.
* **BTH-05-BR-02:** Sự kiện tiền trùng không được cộng hai lần; phương pháp xác định trùng phải dựa trên protocol thật.
* **BTH-05-BR-03:** Không chốt hành vi tiền thừa, hủy, hoàn tiền hay đổi gói khi D-04/D-07 còn mở.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Thiếu tiền|Giữ phiên; hiển thị số còn thiếu và thời gian nếu chính sách đã duyệt |
| Mất thiết bị/mất điện sau nhận tiền|Đưa vào phục hồi; không tự xóa credit |
| Tiền thừa/không rõ tín hiệu|Chuyển quy trình đã duyệt hoặc cần operator; không tự suy đoán |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-05-AC-01:** Given cùng sự kiện nhận tiền được giao lại; When xử lý; Then credit chỉ tăng một lần theo cơ chế định danh đã kiểm chứng.
* **BTH-05-AC-02:** Given đủ tiền đã được ghi bền vững; When UI được mở lại; Then khách không phải trả lại từ đầu.
* **BTH-05-AC-03:** Given khả năng trả lại tiền chưa được cấu hình; When hiển thị thanh toán; Then không có lời hứa trả lại tự động.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-04](../decisions.md#d-04), [D-07](../decisions.md#d-07).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
