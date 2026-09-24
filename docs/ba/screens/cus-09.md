# CUS-09 · Giấy phép và thuê bao

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / quản lý billing |
| Epic tham chiếu | EP-04 |
| Mã màn hình | CUS-09, chưa phải Jira issue key |
| Mục tiêu | Quản lý quyền desktop, seat, thời hạn thuê và quyền cập nhật một cách tách bạch. |

## Câu chuyện người dùng

Là chủ tổ chức / quản lý billing, tôi muốn quản lý quyền desktop, seat, thời hạn thuê và quyền cập nhật một cách tách bạch.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-06](../screens/cus-06.md), [CUS-07](../screens/cus-07.md), [CUS-08](../screens/cus-08.md), [CUS-10](../screens/cus-10.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Giấy phép và thuê bao
Loại quyền • sản phẩm • seat đã dùng/còn lại
Quyền dùng desktop | cập nhật | dịch vụ cloud
Ngày hiệu lực • gia hạn • hủy kỳ tiếp theo
[Gia hạn] [Đổi gói] [Quản lý seat]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| License/entitlement|License service|Không đồng nhất với trạng thái một payment |
| Ngày hiệu lực/kết thúc|Hợp đồng sản phẩm|Vĩnh viễn hiển thị đúng phạm vi thay vì date giả |
| Seat và booth gắn|Device bindings|Có lịch sử chuyển/thu hồi |
| Gia hạn/hủy|Billing subscription|Nêu thời điểm tác động, không gộp hủy dịch vụ với xóa booth |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Gia hạn/đổi gói|Có quyền và sản phẩm cho phép|Review quote và hiệu lực trước xác nhận |
| Hủy gia hạn|Theo điều khoản gói|Hiện ngày tác động và dịch vụ bị ảnh hưởng |
| Chuyển seat|Theo chính sách D-02|Theo dõi thiết bị cũ/mới và xác nhận |

## Luồng chính

1. Xem quyền hiện có.
2. Chọn thao tác hợp lệ.
3. Review tác động tới booth và dịch vụ.
4. Xác nhận rồi theo dõi hiệu lực thực tế.

## Quy tắc nghiệp vụ đề xuất

* **CUS-09-BR-01:** Cloud hết hạn không mặc nhiên hủy quyền desktop vĩnh viễn; phạm vi phải theo D-01.
* **CUS-09-BR-02:** Phiên đang trả tiền không bị ngắt giữa chừng do license refresh theo đề xuất D-03.
* **CUS-09-BR-03:** Không tự tính prorate, hoàn tiền hoặc seat transfer fee khi chưa có policy.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Thuê bao quá hạn|Hiện hạn offline/ảnh hưởng rõ theo chính sách |
| Quyền đang đồng bộ|Tách trạng thái server với booth đã nhận |
| Không có quyền billing|Được xem phần được cấp nhưng không có thao tác thay đổi |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-09-AC-01:** Given license vĩnh viễn và cloud đã hết hạn; When hiển thị; Then hai quyền có trạng thái riêng.
* **CUS-09-AC-02:** Given hủy gia hạn cuối kỳ; When xác nhận; Then UI nêu ngày hiệu lực và không mô tả là hủy ngay nếu policy không phải vậy.
* **CUS-09-AC-03:** Given server đã gia hạn nhưng booth offline; When xem seat; Then hiện chờ booth nhận quyền mới.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-01](../services/svc-01.md), [SVC-02](../services/svc-02.md), [SVC-03](../services/svc-03.md).
* Quyết định còn mở: [D-01](../decisions.md#d-01), [D-02](../decisions.md#d-02), [D-03](../decisions.md#d-03), [D-12](../decisions.md#d-12).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
