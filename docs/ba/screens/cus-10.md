# CUS-10 · Dịch vụ trả phí và mức sử dụng

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / người quản lý dịch vụ |
| Epic tham chiếu | EP-04 |
| Mã màn hình | CUS-10, chưa phải Jira issue key |
| Mục tiêu | Theo dõi AI, lưu trữ/giao file và các dịch vụ đã mua, biết giới hạn trước khi phát sinh phí. |

## Câu chuyện người dùng

Là chủ tổ chức / người quản lý dịch vụ, tôi muốn theo dõi AI, lưu trữ/giao file và các dịch vụ đã mua, biết giới hạn trước khi phát sinh phí.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-08](../screens/cus-08.md), [CUS-09](../screens/cus-09.md), [CUS-11](../screens/cus-11.md), [CUS-13](../screens/cus-13.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Dịch vụ của tổ chức
AI [dùng / quota] • lưu trữ [ ] • delivery [ ]
Kỳ tính phí • sự kiện đang đối soát
Chi tiết cách tính • giới hạn/cảnh báo
[Mua thêm] [Điều chỉnh dịch vụ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Danh mục dịch vụ|Service catalog|Chỉ các dịch vụ thực được bán |
| Usage/quota|Sự kiện metering đã đối soát|Hiện kỳ, đơn vị và độ mới dữ liệu |
| Đang xử lý/reserved|Job service|Không tính là tiêu thụ cuối nếu chưa theo chính sách |
| Overage settings|Chính sách + consent khách|Không mặc định cho phát sinh vượt quota |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Mua thêm|Sản phẩm bổ sung đang bán|Sang checkout có đơn vị và giá rõ |
| Xem chi tiết usage|Có quyền|Liệt kê sự kiện/trạng thái không lộ dữ liệu người khác |

## Luồng chính

1. Xem dịch vụ và kỳ hiện tại.
2. Kiểm tra usage đã xác nhận và phần đang chờ.
3. Xem giới hạn và tác động khi hết quota.
4. Mua thêm hoặc thay đổi theo policy.

## Quy tắc nghiệp vụ đề xuất

* **CUS-10-BR-01:** Cách tính job AI lỗi/hủy, dung lượng và lượt tải là D-09/D-12.
* **CUS-10-BR-02:** Không thu phí hai lần do sự kiện metering được gửi lại.
* **CUS-10-BR-03:** Hết quota cloud không được âm thầm làm hỏng phiên chụp/in local đã trả tiền.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Usage chưa cập nhật|Hiện mốc thời gian, không hứa tức thì |
| Hết quota|Chặn/giảm tính năng theo policy và hiển thị hành động |
| Job lỗi|Hiển thị cách hoàn/quyết toán usage theo chính sách đã duyệt |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-10-AC-01:** Given usage event bị replay; When cộng mức sử dụng; Then không bị tính hai lần.
* **CUS-10-AC-02:** Given không đồng ý overage; When chạm giới hạn; Then không tự tạo phí vượt mức.
* **CUS-10-AC-03:** Given AI job đang chạy; When xem quota; Then phân biệt reserved với đã tiêu thụ nếu mô hình này được chọn.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-03](../services/svc-03.md), [SVC-08](../services/svc-08.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-09](../decisions.md#d-09), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
