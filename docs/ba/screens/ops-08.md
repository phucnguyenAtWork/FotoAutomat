# OPS-08 · Nhật ký kiểm toán và truy cập nội bộ

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Quản trị an toàn / auditor được cấp quyền |
| Epic tham chiếu | EP-02 |
| Mã màn hình | OPS-08, chưa phải Jira issue key |
| Mục tiêu | Truy vết ai đã thay đổi quyền, nội dung, thiết bị hoặc phiên bản và trong phạm vi nào. |

## Câu chuyện người dùng

Là quản trị an toàn / auditor được cấp quyền, tôi muốn truy vết ai đã thay đổi quyền, nội dung, thiết bị hoặc phiên bản và trong phạm vi nào.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-03](../screens/ops-03.md), [OPS-05](../screens/ops-05.md), [OPS-06](../screens/ops-06.md), [OPS-07](../screens/ops-07.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Audit • actor • tenant • target • khoảng thời gian
Sự kiện | trước/sau | lý do | correlation ID
Chi tiết thao tác và nguồn
[Xuất theo quyền] • Chính sách lưu giữ
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Audit event|Các dịch vụ server và booth đồng bộ|Có nguồn thời gian và độ tin cậy |
| Actor/phạm vi|Identity service|Phân biệt người, service account, thiết bị |
| Nội dung thay đổi|Payload đã lọc|Không lưu activation secret/download token/ảnh gốc |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Lọc/xem|Có quyền audit|Chỉ phạm vi được phép |
| Xuất|Có quyền riêng nếu cần|Bản xuất có phạm vi rõ và được ghi audit |

## Luồng chính

1. Chọn tenant hoặc toàn hệ thống theo quyền.
2. Lọc actor/đối tượng.
3. Đọc chuỗi sự kiện liên quan.
4. Xuất phục vụ đối soát nếu được phép.

## Quy tắc nghiệp vụ đề xuất

* **OPS-08-BR-01:** Người sửa nghiệp vụ không được âm thầm sửa/xóa audit của thao tác đó.
* **OPS-08-BR-02:** Lưu giữ audit và dữ liệu cá nhân phải có policy D-10.
* **OPS-08-BR-03:** Audit từ booth offline có thời điểm xảy ra và thời điểm server nhận riêng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Sự kiện chưa đồng bộ|Không kết luận không có thao tác chỉ vì server chưa nhận |
| Không đủ quyền|Không lộ target tenant khác |
| Log lỗi định dạng|Giữ bằng chứng có kiểm soát, không làm trang sập |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-08-AC-01:** Given đổi license; When xem audit; Then thấy actor, lý do, đối tượng và thay đổi trước/sau.
* **OPS-08-AC-02:** Given booth sync sự kiện muộn; When hiển thị; Then phân biệt thời gian tại booth và thời gian nhận.
* **OPS-08-AC-03:** Given token nhạy cảm trong nguồn; When ghi audit; Then không lộ token nguyên văn.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-10](../decisions.md#d-10).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
