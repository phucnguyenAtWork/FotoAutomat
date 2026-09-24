# BTH-07 · Xem và chọn ảnh

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-07, chưa phải Jira issue key |
| Mục tiêu | Chọn các ảnh dùng cho sản phẩm theo giới hạn của gói. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn chọn các ảnh dùng cho sản phẩm theo giới hạn của gói.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-06](../screens/bth-06.md), [BTH-08](../screens/bth-08.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 4 • Chọn ảnh
[Ảnh 1] [Ảnh 2] [Ảnh 3] ...
Đã chọn X / N • Xem lớn
[Chụp lại nếu được phép] [Tiếp tục]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Thumbnail|Ảnh đã lưu của phiên hiện tại|Không tải ảnh phiên khác |
| Số lượng cần chọn|Snapshot gói/khung tương thích|Ghi rõ giới hạn |
| Lựa chọn|Phiên local|Giữ khi quay lại từ bước làm đẹp |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Chọn/bỏ chọn|Trong giới hạn gói|Cập nhật đánh dấu rõ ràng |
| Tiếp tục|Đủ số ảnh theo gói|Mở BTH-08 với các ảnh đã chọn |

## Luồng chính

1. Hiển thị các ảnh có thật trong phiên.
2. Khách xem lớn và chọn ảnh.
3. Kiểm tra số lượng cần thiết.
4. Lưu lựa chọn rồi sang làm đẹp.

## Quy tắc nghiệp vụ đề xuất

* **BTH-07-BR-01:** Ảnh preview có thể nhỏ hơn original nhưng phải tương ứng đúng.
* **BTH-07-BR-02:** Không xóa original khi khách bỏ chọn.
* **BTH-07-BR-03:** Retake giới hạn, chi phí và thời gian phụ thuộc D-08.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Thumbnail lỗi|Hiện placeholder và báo lỗi; không thay bằng ảnh khác |
| Chưa đủ ảnh|Giải thích số ảnh còn cần chọn |
| Offline|Toàn bộ thao tác tiếp tục trên dữ liệu local |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-07-AC-01:** Given chọn chưa đủ số ảnh; When bấm tiếp tục; Then UI nêu số còn thiếu và giữ lựa chọn.
* **BTH-07-AC-02:** Given quay lại từ làm đẹp; When danh sách mở; Then lựa chọn trước đó được giữ.
* **BTH-07-AC-03:** Given ảnh thuộc phiên khác; When truy vấn danh sách; Then ảnh đó không xuất hiện.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-08](../decisions.md#d-08), [D-10](../decisions.md#d-10).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
