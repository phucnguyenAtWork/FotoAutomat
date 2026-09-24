# BTH-06 · Live view và chụp ảnh

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-06, chưa phải Jira issue key |
| Mục tiêu | Giúp khách chuẩn bị tư thế và chụp đủ ảnh theo gói đã thanh toán. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn giúp khách chuẩn bị tư thế và chụp đủ ảnh theo gói đã thanh toán.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-05](../screens/bth-05.md), [BTH-07](../screens/bth-07.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Bước 3 • Chụp ảnh
[ Live view lớn, vùng an toàn khung hình ]
Ảnh 1 / N • Đếm ngược
[Sẵn sàng chụp] [Hỗ trợ]
Thumbnail các ảnh đã nhận
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Live view|Camera adapter|Không hiện hình cũ như tín hiệu live khi camera ngắt |
| Số ảnh cần chụp|Snapshot gói|Không hardcode số ảnh |
| Đếm ngược|Cấu hình phiên|Không bắt đầu khi camera chưa sẵn sàng |
| Ảnh đã lưu|File original nhận thành công|Thumbnail chỉ thêm khi file hợp lệ đã lưu |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Sẵn sàng chụp|Phiên đã thanh toán, camera sẵn sàng|Đếm ngược và trigger một lần |
| Chụp lại|Chỉ khi chính sách gói cho phép|Tạo lần chụp mới có theo dõi, không xóa bằng chứng cũ âm thầm |

## Luồng chính

1. Hiện hướng dẫn tư thế và live view.
2. Khách sẵn sàng; UI đếm ngược.
3. Trigger camera và chờ file được lưu.
4. Lặp theo gói rồi mở màn hình chọn ảnh.

## Quy tắc nghiệp vụ đề xuất

* **BTH-06-BR-01:** Không trigger trước khi phiên được phép chụp.
* **BTH-06-BR-02:** Chạm nhiều lần không tạo nhiều lệnh capture cho cùng bước.
* **BTH-06-BR-03:** Thời hạn giấy phép thay đổi giữa phiên không được ngắt phiên đã trả tiền theo đề xuất D-03.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Camera ngắt trước trigger|Thông báo kết nối lại; bảo toàn phiên đã trả tiền |
| Đã trigger nhưng file chưa rõ|Không tự chụp dồn; chuyển trạng thái cần xác định |
| Lưu file thất bại/disk full|Dừng chụp thêm, giữ thông tin phiên và gọi operator |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-06-AC-01:** Given khách chạm nút hai lần; When bước chụp đang thực hiện; Then chỉ có một lệnh capture cho bước đó.
* **BTH-06-AC-02:** Given camera đã ngắt; When mở live view; Then có trạng thái mất kết nối thay vì hình cũ được gắn nhãn live.
* **BTH-06-AC-03:** Given chưa nhận được file hợp lệ; When camera báo đã trigger; Then UI không tính ảnh đó là đã lưu thành công.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-07](../decisions.md#d-07), [D-08](../decisions.md#d-08), [D-11](../decisions.md#d-11).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
