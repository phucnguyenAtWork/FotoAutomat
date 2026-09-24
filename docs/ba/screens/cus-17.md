# CUS-17 · Thành viên và phân quyền

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / quản trị thành viên |
| Epic tham chiếu | EP-02 |
| Mã màn hình | CUS-17, chưa phải Jira issue key |
| Mục tiêu | Cấp quyền đúng người cho booth, nội dung và billing. |

## Câu chuyện người dùng

Là chủ tổ chức / quản trị thành viên, tôi muốn cấp quyền đúng người cho booth, nội dung và billing.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-01](../screens/cus-01.md), [CUS-02](../screens/cus-02.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Thành viên | [Mời thành viên]
Tên/email | vai trò | phạm vi booth | trạng thái
Chi tiết quyền • lời mời đang chờ
[Đổi quyền] [Thu hồi truy cập]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Thành viên/lời mời|Identity và membership service|Đúng tenant |
| Vai trò|Role catalog được duyệt|Hiển thị quyền cụ thể, không chỉ tên mơ hồ |
| Phạm vi booth|Cấu hình quyền|Tùy chọn theo D-05 |
| Audit thay đổi|Security audit|Có người thao tác và thời điểm |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Mời thành viên|Có quyền và email hợp lệ|Gửi lời mời theo quy trình được duyệt |
| Đổi/thu hồi quyền|Có quyền quản trị tương ứng|Xác nhận tác động, áp dụng ở server |

## Luồng chính

1. Xem thành viên hiện có.
2. Chọn vai trò/phạm vi.
3. Review quyền nhạy cảm như billing/publish.
4. Mời hoặc cập nhật rồi theo dõi trạng thái.

## Quy tắc nghiệp vụ đề xuất

* **CUS-17-BR-01:** Không cho người thường tự nâng quyền.
* **CUS-17-BR-02:** Xử lý owner cuối cùng và chuyển ownership phải được xác định.
* **CUS-17-BR-03:** Support FotoAutomat không là thành viên tenant tự động.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Lời mời hết hạn|Cho gửi lại nếu có quyền |
| Đổi quyền đồng thời|Reload bản mới trước ghi đè |
| Định xóa owner cuối|Chặn và yêu cầu chuyển quyền |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-17-AC-01:** Given operator không có quyền billing; When gọi API đổi thuê bao; Then bị từ chối.
* **CUS-17-AC-02:** Given chỉ còn một owner; When thu hồi owner đó; Then yêu cầu chuyển quyền trước.
* **CUS-17-AC-03:** Given quyền bị thu hồi; When phiên web cũ thao tác nhạy cảm; Then server kiểm tra lại và từ chối.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
