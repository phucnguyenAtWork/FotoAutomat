# CUS-01 · Đăng nhập, đăng ký và khôi phục tài khoản

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ booth / nhân viên tổ chức |
| Epic tham chiếu | EP-02 |
| Mã màn hình | CUS-01, chưa phải Jira issue key |
| Mục tiêu | Vào cổng dịch vụ với đúng tài khoản và tổ chức. |

## Câu chuyện người dùng

Là chủ booth / nhân viên tổ chức, tôi muốn vào cổng dịch vụ với đúng tài khoản và tổ chức.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-02](../screens/cus-02.md), [CUS-03](../screens/cus-03.md), [CUS-17](../screens/cus-17.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
FotoAutomat | Cổng khách hàng
Email [ ]  Phương thức đăng nhập [ ]
[Đăng nhập] [Tạo tài khoản] [Quên mật khẩu]
Thông tin lời mời nếu mở từ email
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Danh tính|Identity service|Phương thức đăng nhập chưa chọn; không tự chốt password/SSO |
| Lời mời|Token mời theo tổ chức|Có hạn và ràng buộc người nhận |
| Tổ chức khả dụng|Quyền thành viên server|Không cho nhập tenant ID để tự cấp quyền |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Đăng nhập|Dữ liệu hợp lệ|Vào chọn tổ chức hoặc onboarding |
| Khôi phục|Theo identity provider được chọn|Thông báo trung tính, không tiết lộ tài khoản tồn tại |

## Luồng chính

1. Mở portal hoặc lời mời.
2. Xác thực danh tính.
3. Chọn tổ chức nếu có nhiều quyền.
4. Vào dashboard hoặc onboarding.

## Quy tắc nghiệp vụ đề xuất

* **CUS-01-BR-01:** Phân quyền phải kiểm tra ở server, không chỉ ẩn menu.
* **CUS-01-BR-02:** Nhân viên FotoAutomat không có quyền toàn hệ thống chỉ vì có tài khoản khách hàng.
* **CUS-01-BR-03:** Không lưu thông tin thẻ thanh toán trong form đăng nhập.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Sai thông tin|Hiện lỗi có thể sửa nhưng không tiết lộ tenant |
| Lời mời hết hạn|Cho yêu cầu gửi lại qua người có quyền |
| Mất mạng|Không hiển thị dữ liệu cache như phiên đăng nhập đã được xác minh mới |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-01-AC-01:** Given thành viên thuộc hai tổ chức; When đăng nhập; Then chỉ thấy hai tổ chức đã được cấp.
* **CUS-01-AC-02:** Given lời mời hết hạn; When chấp nhận; Then không tạo membership mới.
* **CUS-01-AC-03:** Given tài khoản tenant A; When yêu cầu dữ liệu tenant B; Then server từ chối.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
