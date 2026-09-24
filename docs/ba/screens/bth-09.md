# BTH-09 · Chọn khung và xác nhận bản in

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-09, chưa phải Jira issue key |
| Mục tiêu | Xem chính xác bố cục, crop và số bản trước thao tác in khó đảo ngược. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn xem chính xác bố cục, crop và số bản trước thao tác in khó đảo ngược.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-08](../screens/bth-08.md), [BTH-10](../screens/bth-10.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 6 • Chọn khung
Danh sách khung [ ] [ ] [ ]
[ Preview toàn bộ bản in với vùng crop ]
Khổ giấy • số bản • ảnh đã chọn
[Quay lại chỉnh] [Xác nhận in]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Khung|Asset đã publish và tải local|Chỉ khung khớp số ảnh/khổ in |
| Bố cục/crop|Renderer từ lựa chọn phiên|Hiện vùng cắt và không cắt chữ quan trọng |
| Số bản/khổ|Snapshot gói + driver được hỗ trợ|Không tự tăng chi phí sau thanh toán |
| Ảnh xuất|Kết quả render cuối|Phải tồn tại và hợp lệ trước submit |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Chọn khung|Khung tương thích|Tạo preview đúng bố cục |
| Xác nhận in|Render cuối sẵn sàng và printer đủ điều kiện|Ghi ý định in một lần, mở BTH-10 |

## Luồng chính

1. Chọn một khung phù hợp.
2. Xem trước và điều chỉnh crop nếu tính năng được duyệt.
3. Kiểm tra số bản và khổ in.
4. Xác nhận in với thông báo không thể sửa bản đang gửi.

## Quy tắc nghiệp vụ đề xuất

* **BTH-09-BR-01:** Một thao tác xác nhận phải có job ID bền vững.
* **BTH-09-BR-02:** Khung cloud chưa tải hoặc không tương thích không được chọn.
* **BTH-09-BR-03:** Sau khi submit, thay đổi khung không được âm thầm sửa job đã gửi.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Render lỗi|Giữ lựa chọn; không gửi file thiếu cho printer |
| Printer mất sẵn sàng trước submit|Giữ ảnh xuất và gọi hỗ trợ |
| Chạm xác nhận lặp|Hiện job đang có thay vì tạo thêm |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-09-AC-01:** Given xác nhận in bị chạm hai lần; When UI xử lý; Then chỉ tạo một print intent.
* **BTH-09-AC-02:** Given ảnh xuất chưa render xong; When xem CTA; Then nút xác nhận chưa được bật.
* **BTH-09-AC-03:** Given khung sai khổ giấy; When mở danh sách; Then khung không được chọn như một lựa chọn hợp lệ.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-05](../services/svc-05.md), [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-08](../decisions.md#d-08), [D-09](../decisions.md#d-09).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
