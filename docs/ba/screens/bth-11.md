# BTH-11 · Nhận file qua QR và kết thúc

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-07 |
| Mã màn hình | BTH-11, chưa phải Jira issue key |
| Mục tiêu | Nhận file đúng phiên và kết thúc mà không để lộ ảnh cho khách kế tiếp. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn nhận file đúng phiên và kết thúc mà không để lộ ảnh cho khách kế tiếp.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-10](../screens/bth-10.md), [DL-01](../screens/dl-01.md), [BTH-03](../screens/bth-03.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Cảm ơn bạn
[QR đã sẵn sàng] hoặc [File đang chờ tải lên]
Hướng dẫn quét • thời hạn nếu đã xác định
[Kết thúc] • Mã hỗ trợ nếu giao file chậm
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Trạng thái upload|Hàng đợi local + xác nhận server|Tách pending/uploaded/published/failed |
| QR|Delivery service cấp token giới hạn|Không mã hóa đường dẫn công khai đoán được |
| Thời hạn nhận file|Chính sách đã duyệt và server|Không tự đặt số ngày |
| Ảnh hiển thị|Chỉ phiên hiện tại|Xóa khỏi UI khi kết thúc |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Hiện QR|Có link truy cập được hoặc claim link pending được duyệt|Gắn nhãn đúng trạng thái |
| Kết thúc|Luồng cho phép kết thúc|Xóa nội dung khách khỏi màn hình và mở BTH-03 |

## Luồng chính

1. Kiểm tra file đã được publish chưa.
2. Hiện QR sẵn sàng hoặc thông báo giao file chậm.
3. Khách quét và mở DL-01.
4. Kết thúc và đưa UI về màn hình chào sạch.

## Quy tắc nghiệp vụ đề xuất

* **BTH-11-BR-01:** Không hiển thị file đã sẵn sàng khi upload chưa hoàn thành.
* **BTH-11-BR-02:** Nếu dùng claim link offline phải có token được cấp trước; hiện chưa chọn phương án D-06.
* **BTH-11-BR-03:** Không xóa file chưa giao chỉ vì phiên UI đã kết thúc; phải theo chính sách lưu giữ.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Offline chưa có claim link|Hiện chờ tải lên và cách nhận sau đã duyệt; không tạo QR giả |
| Upload lỗi|Giữ trạng thái cần thử lại có kiểm soát |
| Hết thời gian màn hình|Dọn UI theo chính sách; không lộ QR cho khách sau |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-11-AC-01:** Given chưa upload xong; When mở màn hình; Then không hiện nhãn tải ngay cho một link chưa có file.
* **BTH-11-AC-02:** Given bấm kết thúc; When khách mới tới; Then ảnh và QR phiên trước không còn trên UI.
* **BTH-11-AC-03:** Given link đã sẵn sàng; When quét; Then chỉ truy cập bộ ảnh đúng phiên theo quyền token.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-07](../services/svc-07.md).
* Quyết định còn mở: [D-06](../decisions.md#d-06), [D-10](../decisions.md#d-10).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
