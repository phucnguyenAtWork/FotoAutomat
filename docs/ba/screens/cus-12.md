# CUS-12 · Thư viện công thức màu

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / người thiết kế nội dung |
| Epic tham chiếu | EP-05 |
| Mã màn hình | CUS-12, chưa phải Jira issue key |
| Mục tiêu | Quản lý bản nháp, bản đã duyệt và phiên bản màu đang dùng tại booth. |

## Câu chuyện người dùng

Là chủ tổ chức / người thiết kế nội dung, tôi muốn quản lý bản nháp, bản đã duyệt và phiên bản màu đang dùng tại booth.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-13](../screens/cus-13.md), [CUS-15](../screens/cus-15.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Thư viện màu | [Tạo từ ảnh tham khảo]
Lọc: nháp / duyệt / đã phát hành / lưu trữ
[Ảnh mẫu • tên • version • booth đang dùng]
[Chỉnh bản nháp] [Tạo phiên bản mới] [Phân phối]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Recipe và version|Content service|Version đã phát hành là bất biến |
| Ảnh mẫu|Asset được phép sử dụng|Không lấy ảnh khách làm mẫu mặc định |
| Trạng thái duyệt|Workflow nội dung|Phân biệt ready với đã áp dụng trên booth |
| Booth áp dụng|Reported content version|Không suy ra từ danh sách đã chọn để phát hành |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tạo từ tham khảo|Có quyền và quota nếu cần AI|Mở CUS-13 |
| Phát hành|Recipe đã qua kiểm tra tương thích|Mở CUS-15 |

## Luồng chính

1. Xem công thức hiện có.
2. Mở một bản nháp hoặc tạo mới.
3. Xem lịch sử phiên bản và kết quả thử.
4. Chọn phát hành bản đã duyệt.

## Quy tắc nghiệp vụ đề xuất

* **CUS-12-BR-01:** Sửa recipe đang phát hành tạo revision mới.
* **CUS-12-BR-02:** Không khẳng định AI tái tạo chính xác màu chỉ từ một ảnh tham khảo.
* **CUS-12-BR-03:** Người thiết kế có thể khác người được phép phát hành.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Thư viện rỗng|Dẫn đến tạo công thức đầu tiên |
| Recipe không tương thích booth|Có nhãn và lý do, không âm thầm đổi renderer |
| Không có quyền sửa|Chế độ xem theo quyền |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-12-AC-01:** Given recipe version đang phục vụ; When chỉnh sửa; Then tạo bản mới và không đổi version đang dùng.
* **CUS-12-AC-02:** Given booth chưa báo áp dụng; When xem recipe; Then không tính booth đó là đã dùng bản mới.
* **CUS-12-AC-03:** Given không có quyền publish; When thao tác; Then không thể phát hành qua UI/API.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-05](../services/svc-05.md), [SVC-08](../services/svc-08.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-09](../decisions.md#d-09).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
