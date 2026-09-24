# BTH-01 · Cài đặt và kích hoạt booth

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Chủ booth / kỹ thuật viên được cấp quyền |
| Epic tham chiếu | EP-02 |
| Mã màn hình | BTH-01, chưa phải Jira issue key |
| Mục tiêu | Gắn bản cài Windows với đúng tổ chức và một quyền sử dụng còn khả dụng. |

## Câu chuyện người dùng

Là chủ booth / kỹ thuật viên được cấp quyền, tôi muốn gắn bản cài Windows với đúng tổ chức và một quyền sử dụng còn khả dụng.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-06](../screens/cus-06.md), [BTH-02](../screens/bth-02.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Logo FotoAutomat | Thiết lập thiết bị
Tên booth [                 ]
Mã kích hoạt [              ] [Xác minh]
Tổ chức • sản phẩm • quyền được cấp
[Kích hoạt trên máy này] → [Thiết lập thiết bị]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Tên booth|Người cài đặt nhập|Bắt buộc; giới hạn ký tự theo cấu hình; không thay thế mã thiết bị |
| Mã kích hoạt|CUS-06|Che nội dung nhạy cảm; không ghi log |
| Tổ chức và quyền sử dụng|Máy chủ xác minh mã|Chỉ đọc; hiển thị trước khi xác nhận |
| Device ID / phiên bản|Đăng ký thiết bị và bản cài|Có nút sao chép mã hỗ trợ; không lộ credential |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Xác minh mã|Có mã hợp lệ về định dạng và có mạng|Hiển thị đúng tổ chức, sản phẩm và khả năng cấp seat |
| Kích hoạt|Đã xác minh và chủ thể được phép|Ràng buộc thiết bị; tải quyền đã ký; mở BTH-02 |

## Luồng chính

1. Mở bản cài chưa đăng ký.
2. Nhập tên và mã do cổng khách hàng cấp.
3. Xem tổ chức và giấy phép sẽ sử dụng.
4. Xác nhận rồi nhận kết quả đăng ký trước khi cấu hình thiết bị.

## Quy tắc nghiệp vụ đề xuất

* **BTH-01-BR-01:** Kích hoạt lần đầu cần máy chủ; đề xuất không bán phiên chụp khi chưa đăng ký.
* **BTH-01-BR-02:** Gửi lại cùng yêu cầu sau timeout phải trả kết quả cũ, không chiếm thêm seat.
* **BTH-01-BR-03:** Chuyển booth sang tổ chức khác là quy trình thu hồi/chuyển giao có quyền riêng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Mất mạng|Giữ tên booth; cho thử xác minh lại; không báo đã kích hoạt |
| Mã hết hạn/đã dùng/sai tổ chức|Thông báo có hướng xử lý; không tiết lộ tenant khác |
| Đã đăng ký|Hiện tổ chức và ID hiện tại; không âm thầm đăng ký lần hai |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-01-AC-01:** Given mã hợp lệ và còn seat; When xác nhận kích hoạt; Then portal thấy một booth mới cùng Device ID và booth chuyển sang cấu hình.
* **BTH-01-AC-02:** Given máy chủ đã cấp thiết bị nhưng phản hồi bị mất; When gửi lại request ID; Then không tạo booth hoặc chiếm seat lần hai.
* **BTH-01-AC-03:** Given máy chưa kích hoạt và mất mạng; When mở ứng dụng; Then UI không cho vào luồng nhận tiền.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-01](../services/svc-01.md), [SVC-02](../services/svc-02.md).
* Quyết định còn mở: [D-02](../decisions.md#d-02), [D-03](../decisions.md#d-03).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
