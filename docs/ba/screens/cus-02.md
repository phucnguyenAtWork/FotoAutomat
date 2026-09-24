# CUS-02 · Thiết lập tổ chức khách hàng

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức |
| Epic tham chiếu | EP-02 |
| Mã màn hình | CUS-02, chưa phải Jira issue key |
| Mục tiêu | Tạo hồ sơ đơn vị mua/thuê phần mềm trước khi đăng ký booth hoặc dịch vụ. |

## Câu chuyện người dùng

Là chủ tổ chức, tôi muốn tạo hồ sơ đơn vị mua/thuê phần mềm trước khi đăng ký booth hoặc dịch vụ.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-01](../screens/cus-01.md), [CUS-03](../screens/cus-03.md), [CUS-07](../screens/cus-07.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Chào mừng • Thiết lập đơn vị
Tên đơn vị [ ] • liên hệ [ ]
Múi giờ [ ] • thông tin thanh toán cần thiết
[Lưu và tiếp tục]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Tên đơn vị|Người sở hữu nhập|Bắt buộc; không đồng nghĩa mã pháp lý |
| Liên hệ hỗ trợ|Chủ tổ chức nhập|Chỉ thu thập trường có mục đích |
| Múi giờ|Cấu hình tổ chức|Dùng cho báo cáo và lịch cập nhật |
| Hồ sơ xuất chứng từ|Khách cung cấp theo thị trường|Trường bắt buộc phụ thuộc D-12 |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tạo tổ chức|Có danh tính và quyền tạo|Cấp owner ban đầu, ghi audit |
| Tiếp tục|Đã lưu tổ chức|Mở CUS-07 hoặc CUS-03 |

## Luồng chính

1. Nhập thông tin tổ chức.
2. Chọn múi giờ.
3. Xem lại hồ sơ.
4. Tạo tổ chức một lần rồi chuyển sang chọn sản phẩm.

## Quy tắc nghiệp vụ đề xuất

* **CUS-02-BR-01:** Request lặp không được tạo hai tổ chức ngoài ý muốn.
* **CUS-02-BR-02:** Thông tin billing tách khỏi thông tin hiển thị công khai.
* **CUS-02-BR-03:** Chuyển quyền owner cần luồng xác nhận riêng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Chưa có dữ liệu|Hiển thị form onboarding thay dashboard trống |
| Tạo timeout|Cho kiểm tra kết quả hoặc gửi lại cùng request |
| Không có quyền tạo thêm|Giải thích và cho chọn tổ chức đã có |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-02-AC-01:** Given request tạo tổ chức bị gửi lại; When xử lý cùng request ID; Then chỉ một tổ chức được tạo.
* **CUS-02-AC-02:** Given chưa chọn múi giờ; When lưu; Then form chỉ rõ trường cần hoàn thành.
* **CUS-02-AC-03:** Given thành viên thường; When sửa hồ sơ billing; Then phải có quyền tương ứng.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
