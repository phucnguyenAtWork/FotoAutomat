# CUS-14 · Thư viện và chỉnh sửa khung ảnh

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Người thiết kế nội dung |
| Epic tham chiếu | EP-05 |
| Mã màn hình | CUS-14, chưa phải Jira issue key |
| Mục tiêu | Tạo bố cục in tương thích khổ giấy và số ảnh. |

## Câu chuyện người dùng

Là người thiết kế nội dung, tôi muốn tạo bố cục in tương thích khổ giấy và số ảnh.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-15](../screens/cus-15.md), [BTH-09](../screens/bth-09.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Khung ảnh | [Tạo khung]
Danh sách khung • trạng thái • khổ giấy
Canvas: vùng ảnh / artwork / vùng cắt an toàn
Thuộc tính kích thước • slots • preview
[Lưu nháp] [Duyệt phiên bản]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Kích thước và vùng cắt|Profile khổ in đã duyệt|Đơn vị hiển thị rõ; không nhầm pixel với mm |
| Slots ảnh|Người thiết kế đặt|Số lượng và tỷ lệ phải khớp cấu hình hỗ trợ |
| Artwork|Asset upload|Kiểm tra định dạng, kích thước và quyền sử dụng |
| Preview|Renderer theo template version|Có mẫu crop và safe area |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tạo/chỉnh khung|Có quyền nội dung|Lưu bản nháp |
| Duyệt|Qua validation và preview|Tạo version có thể chọn ở publish |

## Luồng chính

1. Chọn khổ giấy.
2. Tạo vùng ảnh và artwork.
3. Kiểm tra crop bằng ảnh mẫu.
4. Lưu và duyệt version.

## Quy tắc nghiệp vụ đề xuất

* **CUS-14-BR-01:** Bản đang phát hành không sửa trực tiếp.
* **CUS-14-BR-02:** Profile in phải được kiểm chứng trên DNP/driver thật.
* **CUS-14-BR-03:** Không cho template tải hoặc thực thi code từ nguồn ngoài.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Sai kích thước|Chỉ rõ vùng/trường sai |
| Thiếu asset|Không cho duyệt |
| Khung đang có booth dùng|Cho lưu version mới, không xóa asset đang được tham chiếu |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-14-AC-01:** Given template vượt vùng in được hỗ trợ; When duyệt; Then bị chặn với lỗi cụ thể.
* **CUS-14-AC-02:** Given sửa một khung đã publish; When lưu; Then tạo revision mới.
* **CUS-14-AC-03:** Given thiếu artwork đã tham chiếu; When phát hành; Then gói nội dung không được đánh dấu hoàn chỉnh.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-05](../services/svc-05.md).
* Quyết định còn mở: [D-08](../decisions.md#d-08), [D-09](../decisions.md#d-09).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
