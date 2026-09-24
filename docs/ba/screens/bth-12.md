# BTH-12 · Vận hành cục bộ và xử lý phiên gián đoạn

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Vận hành booth / chủ booth được phép |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-12, chưa phải Jira issue key |
| Mục tiêu | Kiểm tra sức khỏe booth và xử lý phiên tiền/in không rõ kết quả ngay tại máy. |

## Câu chuyện người dùng

Là vận hành booth / chủ booth được phép, tôi muốn kiểm tra sức khỏe booth và xử lý phiên tiền/in không rõ kết quả ngay tại máy.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-02](../screens/bth-02.md), [BTH-03](../screens/bth-03.md), [CUS-05](../screens/cus-05.md), [OPS-06](../screens/ops-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Chế độ vận hành | Xác thực operator
Thiết bị • mạng • dung lượng • quyền offline
Phiên cần xử lý [mã / tiền / job / lý do]
Chi tiết bằng chứng • lịch sử thao tác
[Ghi nhận kết quả] [Kiểm tra thiết bị] [Đóng chế độ vận hành]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Danh sách sự cố|Local store|Có ID và thời gian; chưa đồng bộ phải được đánh dấu |
| Bằng chứng tiền/in|Adapter events + intent + operator notes|Phân biệt dữ liệu thiết bị với xác nhận của người |
| Quyền offline|Entitlement local|Hiển thị thời điểm xác minh và hạn hiệu lực nếu có |
| Kết quả xử lý|Operator nhập và quyền hiện tại|Bắt buộc lý do; không cho sửa lịch sử gốc |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Ghi nhận kết quả|Có quyền và bằng chứng/lý do|Tạo quyết định đối soát có audit |
| In lại có kiểm soát|Chỉ theo chính sách và operator xác nhận riêng|Tạo job mới liên kết job cũ, không sửa job cũ thành chưa in |
| Mở bán lại|Các điều kiện phục vụ đạt|Về BTH-03 |

## Luồng chính

1. Operator vào chế độ được bảo vệ.
2. Xem sự cố và bằng chứng.
3. Xác định khách đã nhận tiền/in/file đến đâu.
4. Ghi quyết định xử lý và chỉ mở lại booth khi phù hợp.

## Quy tắc nghiệp vụ đề xuất

* **BTH-12-BR-01:** Cách xác thực operator offline cần được thiết kế D-05.
* **BTH-12-BR-02:** Không cho khách tự mở quyền hoàn tiền hay in lại.
* **BTH-12-BR-03:** Máy chủ mất kết nối không ngăn operator xem bằng chứng local.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Offline|Cho thao tác trong quyền local đã duyệt; audit xếp hàng đồng bộ |
| Thiếu bằng chứng|Giữ NeedsReview; không tự đánh dấu hoàn tất |
| Hết quyền operator|Chặn thao tác nhạy cảm và đưa hướng liên hệ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-12-AC-01:** Given không có quyền operator; When mở trang; Then không thấy điều khiển tiền/in lại.
* **BTH-12-AC-02:** Given operator xác nhận in lại được phép; When thực hiện; Then tạo job mới có tham chiếu job cũ và lý do.
* **BTH-12-AC-03:** Given mất mạng; When xem phiên gián đoạn; Then bằng chứng local vẫn đọc được và audit chưa sync được ghi rõ.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-04](../decisions.md#d-04), [D-05](../decisions.md#d-05), [D-08](../decisions.md#d-08).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
