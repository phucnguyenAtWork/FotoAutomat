# BTH-03 · Màn hình chào và trạng thái phục vụ

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-03, chưa phải Jira issue key |
| Mục tiêu | Cho khách biết booth có thể phục vụ và bắt đầu một phiên mới. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn cho khách biết booth có thể phục vụ và bắt đầu một phiên mới.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-04](../screens/bth-04.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Thương hiệu / hình minh họa
Chụp ảnh theo phong cách của bạn
Thông tin giá khởi điểm nếu đã cấu hình
[Bắt đầu chụp]
Ngôn ngữ [ ] • Hỗ trợ
Thông báo bảo trì hoặc giao file chậm nếu có
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Nội dung chào|Gói nội dung đã tải và duyệt|Hoạt động không cần Internet |
| Trạng thái sẵn sàng|Thiết bị + quyền local + dung lượng|Không suy ra từ heartbeat cũ trên server |
| Giá giới thiệu|Bảng giá đã áp dụng|Không hiện giá chưa được áp dụng tại booth |
| Ngôn ngữ|Danh sách ngôn ngữ được hỗ trợ|Không tự suy ra từ camera/Windows |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Bắt đầu chụp|Booth sẵn sàng và không có phiên khác|Tạo phiên mới và mở BTH-04 |
| Hỗ trợ|Luôn có khi đang lỗi|Hiện cách liên hệ phù hợp tại booth |

## Luồng chính

1. Booth vào chế độ chờ sau khi kiểm tra sẵn sàng.
2. Khách xem thông tin dịch vụ.
3. Khách chọn ngôn ngữ nếu cần.
4. Bắt đầu một phiên mới.

## Quy tắc nghiệp vụ đề xuất

* **BTH-03-BR-01:** Màn hình chờ không được còn ảnh hoặc mã QR của khách trước.
* **BTH-03-BR-02:** Mất mạng không tự làm booth ngừng chụp/in khi quyền offline còn hợp lệ.
* **BTH-03-BR-03:** Lỗi thiết bị bắt buộc hoặc quyền không cho phiên mới phải chặn CTA bắt đầu.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Booth bảo trì|Thay CTA bằng thông báo ngắn và kênh hỗ trợ |
| Mạng gián đoạn|Thông báo nhận file có thể chậm trước khi khách thanh toán |
| Phiên trước chưa xử lý|Chuyển trạng thái tạm ngừng; không ghép khách mới vào phiên cũ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-03-AC-01:** Given phiên trước đã kết thúc; When màn hình chào xuất hiện; Then không hiển thị ảnh hay token của phiên trước.
* **BTH-03-AC-02:** Given mất mạng nhưng các điều kiện local hợp lệ; When khách bắt đầu; Then vẫn vào chọn gói.
* **BTH-03-AC-03:** Given máy in không thể phục vụ; When khách nhìn màn hình chào; Then nút bắt đầu bị vô hiệu hóa và có hướng dẫn.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-02](../services/svc-02.md), [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-03](../decisions.md#d-03), [D-06](../decisions.md#d-06), [D-10](../decisions.md#d-10).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
