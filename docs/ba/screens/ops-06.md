# OPS-06 · Sự cố và lịch sử lệnh từ xa

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Support/operations theo phạm vi được cấp |
| Epic tham chiếu | EP-03 |
| Mã màn hình | OPS-06, chưa phải Jira issue key |
| Mục tiêu | Đối soát yêu cầu và trạng thái thực thi ở booth, xử lý sự cố không rõ kết quả. |

## Câu chuyện người dùng

Là support/operations theo phạm vi được cấp, tôi muốn đối soát yêu cầu và trạng thái thực thi ở booth, xử lý sự cố không rõ kết quả.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-01](../screens/ops-01.md), [OPS-02](../screens/ops-02.md), [OPS-08](../screens/ops-08.md), [CUS-18](../screens/cus-18.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Hàng đợi sự cố • mức ảnh hưởng • tenant
Command/incident | booth | requested/received/result
Timeline bằng chứng • log đã lọc
Hành động được phép • lý do • xác nhận
[Phân công] [Đóng có căn cứ] [Tạo lệnh mới]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Command timeline|Command service + ack booth|Lưu request ID, hết hạn, received và kết quả tách biệt |
| Sự cố|Telemetry/support requests|Có tenant và mức ảnh hưởng |
| Diagnostic|Dữ liệu đã lọc|Không chứa token/ảnh khách mặc định |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tạo lệnh cho phép|Quyền hợp lệ và target đúng|Review tác động; cấp ID mới |
| Đóng sự cố|Có bằng chứng hoặc lý do đối soát|Ghi audit, không sửa nguyên bản sự kiện |
| Thử lại|Theo tính chất tác vụ|Không retry mù lệnh có tác dụng tiền/in |

## Luồng chính

1. Mở incident.
2. Xem dấu thời gian và các ack.
3. Phân biệt chưa giao với kết quả chưa rõ.
4. Thực hiện hành động được phép và ghi kết quả.

## Quy tắc nghiệp vụ đề xuất

* **OPS-06-BR-01:** Danh mục lệnh cho phép rõ ràng; không cung cấp arbitrary remote shell.
* **OPS-06-BR-02:** Kết quả unknown của thao tác có side effect cần đối soát.
* **OPS-06-BR-03:** Support ticket không tự nâng quyền truy cập tenant.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Booth offline|Pending delivery, có TTL |
| Ack mất sau thực thi|Unknown/reconcile, không bấm retry như chưa chạy |
| Quyền bị thu hồi|Chặn lệnh mới ngay ở server |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-06-AC-01:** Given command hết TTL; When booth nhận muộn; Then từ chối thực thi.
* **OPS-06-AC-02:** Given ack kết quả in mất; When support mở incident; Then không có tự động reprint.
* **OPS-06-AC-03:** Given tạo command; When xem audit; Then có actor, tenant, target, lý do và correlation ID.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-09](../services/svc-09.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-13](../decisions.md#d-13).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
