# OPS-02 · Hồ sơ khách hàng thương mại

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Staff account/support theo quyền |
| Epic tham chiếu | EP-04 |
| Mã màn hình | OPS-02, chưa phải Jira issue key |
| Mục tiêu | Xem quan hệ giữa tổ chức, người quản trị, booth và sản phẩm đã mua. |

## Câu chuyện người dùng

Là staff account/support theo quyền, tôi muốn xem quan hệ giữa tổ chức, người quản trị, booth và sản phẩm đã mua.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-03](../screens/ops-03.md), [OPS-06](../screens/ops-06.md), [OPS-07](../screens/ops-07.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Khách hàng | Tìm tên/mã tổ chức
Tổng quan tổ chức • liên hệ • người quản trị
Booth | giấy phép | dịch vụ | yêu cầu hỗ trợ
Lịch sử thay đổi và đối soát
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Tenant profile|Tenant registry|Mã ổn định; không gộp tenant chỉ vì trùng email liên hệ |
| Sản phẩm/booth|License và device registry|Chỉ hiển thị trong quyền staff |
| Lịch sử hỗ trợ|Support service|Dữ liệu tối thiểu cần thiết |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Xem quyền/booth|Có permission phù hợp|Đi đến tài nguyên đúng tenant |
| Đề xuất thay đổi hồ sơ|Có quyền|Ghi audit và không sửa chứng từ đã phát hành |

## Luồng chính

1. Tìm tổ chức.
2. Xác minh đúng mã khách hàng.
3. Xem sản phẩm và thiết bị liên quan.
4. Chuyển sang quy trình hỗ trợ hoặc giấy phép.

## Quy tắc nghiệp vụ đề xuất

* **OPS-02-BR-01:** Truy cập toàn hệ thống phải có audit phù hợp.
* **OPS-02-BR-02:** Đình chỉ cloud và thu hồi quyền desktop là thao tác khác nhau.
* **OPS-02-BR-03:** Xóa tổ chức hoặc chuyển booth không là thao tác một chạm ở trang hồ sơ.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Không tìm thấy|Hiện empty state không gợi ý dữ liệu ngoài quyền |
| Dữ liệu billing lệch|Đánh dấu cần đối soát, không tự sửa ledger |
| Khách bị hạn chế|Hiển thị lý do/phạm vi theo quyền |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-02-AC-01:** Given hai tổ chức cùng tên; When chọn hồ sơ; Then mã tenant được hiển thị để phân biệt.
* **OPS-02-AC-02:** Given nhân viên support không có quyền billing; When thao tác giao dịch; Then bị từ chối.
* **OPS-02-AC-03:** Given license và payment lệch trạng thái; When mở hồ sơ; Then hiện cần đối soát thay tự cấp/thu hồi.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-01](../services/svc-01.md), [SVC-02](../services/svc-02.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-05](../decisions.md#d-05), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
