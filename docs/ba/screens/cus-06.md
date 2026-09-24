# CUS-06 · Tải phần mềm và cấp mã kích hoạt

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / quản lý thiết bị |
| Epic tham chiếu | EP-02 |
| Mã màn hình | CUS-06, chưa phải Jira issue key |
| Mục tiêu | Tải đúng bộ cài và cấp quyền cài đặt có thể theo dõi. |

## Câu chuyện người dùng

Là chủ tổ chức / quản lý thiết bị, tôi muốn tải đúng bộ cài và cấp quyền cài đặt có thể theo dõi.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-07](../screens/cus-07.md), [CUS-09](../screens/cus-09.md), [BTH-01](../screens/bth-01.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Cài đặt FotoAutomat
Bản khuyến nghị • Windows hỗ trợ • release notes
[Tải bộ cài]
Seat còn lại • booth đã gắn
[Tạo mã kích hoạt] → hướng dẫn cài trên máy booth
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Bộ cài|Release catalog hợp lệ|Có version, checksum/thông tin ký và điều kiện hỗ trợ |
| Seat khả dụng|License service|Theo gói thực tế, không chỉ count giao diện |
| Mã kích hoạt|Enrollment service|Chỉ xem/sao chép theo quyền; có hạn, không lộ trong audit |
| Hướng dẫn cài|Tài liệu theo release|Không yêu cầu cài SDK phát triển lên booth thương mại |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tải bộ cài|Có quyền truy cập bản phát hành|Tải artifact đúng phiên bản |
| Tạo mã|Đủ quyền và chính sách seat|Cấp code có thời hạn |
| Thu hồi mã chưa dùng|Có quyền|Không ảnh hưởng máy đã kích hoạt ngoài chính sách |

## Luồng chính

1. Chọn release đủ điều kiện.
2. Tải và cài trên Windows.
3. Tạo mã dùng cho tổ chức.
4. Nhập mã ở BTH-01 rồi theo dõi thiết bị xuất hiện.

## Quy tắc nghiệp vụ đề xuất

* **CUS-06-BR-01:** Tải được installer không đồng nghĩa được cấp thêm license.
* **CUS-06-BR-02:** Không dùng một activation code vĩnh viễn chung cho mọi khách.
* **CUS-06-BR-03:** Quyền nâng cấp của giấy phép mua vĩnh viễn phải kiểm tra riêng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Không còn seat|Giải thích và dẫn tới CUS-09/CUS-07 |
| Mã hết hạn|Cho tạo mã mới theo quyền |
| Release bị thu hồi|Không cấp link tải mới; hướng dẫn bản thay thế |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-06-AC-01:** Given hết seat; When tạo mã dùng để thêm booth; Then hệ thống từ chối hoặc theo chính sách giữ chỗ đã duyệt, không cấp vượt.
* **CUS-06-AC-02:** Given release bị thu hồi; When tải; Then không lấy được bộ cài qua link mới.
* **CUS-06-AC-03:** Given mã dùng thành công; When xem portal; Then thấy Device ID và thời điểm sử dụng.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-01](../services/svc-01.md), [SVC-02](../services/svc-02.md), [SVC-06](../services/svc-06.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-02](../decisions.md#d-02), [D-13](../decisions.md#d-13).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
