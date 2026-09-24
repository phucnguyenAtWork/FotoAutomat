# BTH-08 · Làm đẹp sau chụp

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-08, chưa phải Jira issue key |
| Mục tiêu | Điều chỉnh mức làm đẹp và xem trước kết quả mà không mất ảnh gốc. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn điều chỉnh mức làm đẹp và xem trước kết quả mà không mất ảnh gốc.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-07](../screens/bth-07.md), [BTH-09](../screens/bth-09.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 5 • Làm đẹp
[ So sánh trước / sau ]
Mức làm đẹp [0 -------- tối đa được cấu hình]
[Đặt lại] [Áp dụng] [Tiếp tục]
Trạng thái đang xử lý
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Ảnh trước|Original + color recipe đã chọn|Không ghi đè original |
| Ảnh sau|Kết quả render theo revision hiện tại|Kết quả cũ không ghi đè yêu cầu mới |
| Mức làm đẹp|Lựa chọn khách trong khoảng được hỗ trợ|Mặc định và giới hạn cần duyệt |
| Trạng thái xử lý|Local image pipeline|Có phản hồi khi tốn thời gian; không khóa toàn UI |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Điều chỉnh|Có ảnh hợp lệ|Tạo yêu cầu preview giới hạn tài nguyên |
| Tiếp tục|Kết quả tương ứng revision hiện tại sẵn sàng|Lưu thông số và sang BTH-09 |

## Luồng chính

1. Hiện ảnh với màu đã chọn.
2. Khách điều chỉnh mức làm đẹp.
3. So sánh trước/sau và có thể đặt lại.
4. Xác nhận thông số cho ảnh xuất.

## Quy tắc nghiệp vụ đề xuất

* **BTH-08-BR-01:** Làm đẹp phải hoạt động offline theo yêu cầu sản phẩm.
* **BTH-08-BR-02:** Không mặc định có GPU rời; phải kiểm chứng pipeline trên cấu hình thấp.
* **BTH-08-BR-03:** Preview và ảnh in phải sử dụng cùng recipe/beauty revision dù độ phân giải khác nhau.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Xử lý chậm|Hiện đang xử lý; gộp thay đổi liên tiếp, không xếp vô hạn job |
| Pipeline lỗi|Cho trở về ảnh không làm đẹp hoặc hỗ trợ theo chính sách duyệt |
| Không phát hiện khuôn mặt|Giải thích và cho tiếp tục không làm đẹp |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-08-AC-01:** Given khách đổi slider trong khi job cũ chạy; When job cũ trả kết quả sau; Then nó không ghi đè preview revision mới.
* **BTH-08-AC-02:** Given mất mạng; When chọn mức làm đẹp; Then pipeline được hỗ trợ vẫn chạy tại booth.
* **BTH-08-AC-03:** Given đặt lại; When xác nhận; Then original không đổi và thông số beauty trở về mức đã định nghĩa.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-08](../services/svc-08.md), [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-08](../decisions.md#d-08), [D-09](../decisions.md#d-09), [D-11](../decisions.md#d-11).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
