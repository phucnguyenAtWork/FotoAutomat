# OPS-01 · Tổng quan đội booth toàn hệ thống

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Nhân sự FotoAutomat có quyền vận hành fleet |
| Epic tham chiếu | EP-03 |
| Mã màn hình | OPS-01, chưa phải Jira issue key |
| Mục tiêu | Theo dõi độ phủ phiên bản, sức khỏe và các booth mất liên lạc trên toàn hệ thống. |

## Câu chuyện người dùng

Là nhân sự fotoautomat có quyền vận hành fleet, tôi muốn theo dõi độ phủ phiên bản, sức khỏe và các booth mất liên lạc trên toàn hệ thống.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-02](../screens/ops-02.md), [OPS-05](../screens/ops-05.md), [OPS-06](../screens/ops-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
FotoAutomat Operations | Fleet
Booth theo tenant • trạng thái • phiên bản
Cảnh báo lỗi hàng loạt / mất liên lạc
Danh sách booth • liên lạc cuối • lỗi
[Xem chi tiết] [Xem đợt cập nhật]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Fleet registry|Device service toàn hệ thống theo quyền staff|Có tenant trên mọi dòng |
| Health/last seen|Telemetry|Ghi rõ thời điểm quan sát và độ mới |
| Độ phủ phiên bản|Reported version|Không dùng desired version làm số đã nâng cấp |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Mở booth|Có quyền support/operations trong phạm vi|Hiện chi tiết với nhãn tenant rõ |
| Lọc phiên bản lỗi|Có dữ liệu telemetry|Tạo tập xem, chưa gửi lệnh hàng loạt |

## Luồng chính

1. Đăng nhập staff qua cơ chế được duyệt.
2. Xem tình trạng và phiên bản.
3. Lọc nhóm bất thường.
4. Mở incident hoặc kế hoạch cập nhật liên quan.

## Quy tắc nghiệp vụ đề xuất

* **OPS-01-BR-01:** Đây là quyền nội bộ FotoAutomat, không phải dashboard owner tenant.
* **OPS-01-BR-02:** Số offline dựa trên ngưỡng heartbeat được định nghĩa, không chứng minh máy tắt.
* **OPS-01-BR-03:** Trang không cần xem ảnh khách để biết booth có hoạt động.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Telemetry trễ diện rộng|Hiện cảnh báo chất lượng dữ liệu |
| Không đủ quyền staff|Từ chối truy cập dù có tài khoản khách hàng |
| Một nguồn metrics lỗi|Không chuyển giá trị thành 0 |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-01-AC-01:** Given chỉ có tài khoản khách hàng; When mở Ops; Then không được xem fleet toàn hệ thống.
* **OPS-01-AC-02:** Given rollout target đã đặt nhưng máy chưa báo version; When tính coverage; Then máy chưa được tính là đã nâng cấp.
* **OPS-01-AC-03:** Given heartbeat quá ngưỡng; When hiển thị; Then có last seen và nhãn mất liên lạc.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
