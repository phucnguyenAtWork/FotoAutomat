# CUS-03 · Tổng quan tài khoản và booth

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ booth / quản lý tổ chức |
| Epic tham chiếu | EP-03 |
| Mã màn hình | CUS-03, chưa phải Jira issue key |
| Mục tiêu | Nhìn nhanh booth, giấy phép, dịch vụ và vấn đề cần xử lý của tổ chức. |

## Câu chuyện người dùng

Là chủ booth / quản lý tổ chức, tôi muốn nhìn nhanh booth, giấy phép, dịch vụ và vấn đề cần xử lý của tổ chức.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-04](../screens/cus-04.md), [CUS-06](../screens/cus-06.md), [CUS-09](../screens/cus-09.md), [CUS-10](../screens/cus-10.md), [CUS-16](../screens/cus-16.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Tổ chức [ ] | Tổng quan
Booth hoạt động • mất liên lạc • cần xử lý
Quyền sử dụng • dịch vụ sắp hết hạn
Cảnh báo và tác vụ gần đây
[Thêm booth] [Quản lý dịch vụ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Chỉ số booth|Heartbeat và trạng thái báo về|Có thời điểm cập nhật và tiêu chí trạng thái |
| Giấy phép|Entitlement service|Tách thuê/vĩnh viễn/dịch vụ |
| Cảnh báo|Incident service|Chỉ tenant hiện tại |
| Số liệu phiên|Session đã đồng bộ|Hiện khoảng thời gian và mức độ đầy đủ |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Xem booth lỗi|Có quyền xem thiết bị|Mở danh sách đã lọc |
| Thêm booth|Có quyền quản lý|Mở tải/kích hoạt hoặc mua quyền nếu thiếu |

## Luồng chính

1. Vào đúng tổ chức.
2. Xem tình trạng chung và độ mới dữ liệu.
3. Ưu tiên sự cố ảnh hưởng phục vụ.
4. Mở màn hình chi tiết liên quan.

## Quy tắc nghiệp vụ đề xuất

* **CUS-03-BR-01:** Mất heartbeat có nghĩa chưa liên lạc được, không chứng minh máy đã tắt.
* **CUS-03-BR-02:** Doanh thu chụp của khách và phí FotoAutomat phải khác nhãn.
* **CUS-03-BR-03:** Không cộng dữ liệu chưa đồng bộ vào số liệu đã xác nhận.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Chưa có booth|Hướng dẫn thêm booth với CTA rõ |
| Dữ liệu cũ|Hiện thời điểm báo về gần nhất |
| Lỗi một widget|Widget khác còn dùng được; widget lỗi không hiển thị số 0 giả |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-03-AC-01:** Given booth mất mạng; When mở dashboard; Then hiện lần liên lạc cuối và trạng thái chưa cập nhật.
* **CUS-03-AC-02:** Given chưa có booth; When vào dashboard; Then thấy hướng dẫn kích hoạt.
* **CUS-03-AC-03:** Given bộ đếm truy vấn lỗi; When render; Then không thay lỗi bằng số 0.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
