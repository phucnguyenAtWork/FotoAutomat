# CUS-04 · Danh sách booth

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ booth / quản lý / người xem được cấp quyền |
| Epic tham chiếu | EP-03 |
| Mã màn hình | CUS-04, chưa phải Jira issue key |
| Mục tiêu | Tìm, nhóm và so sánh các booth thuộc tổ chức. |

## Câu chuyện người dùng

Là chủ booth / quản lý / người xem được cấp quyền, tôi muốn tìm, nhóm và so sánh các booth thuộc tổ chức.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-03](../screens/cus-03.md), [CUS-05](../screens/cus-05.md), [CUS-06](../screens/cus-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Booth | [Thêm booth]
Tìm tên/ID [ ] • địa điểm • trạng thái • phiên bản
Tên | vị trí | liên lạc cuối | phiên bản thực | quyền
[Mở chi tiết] • phân trang
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Danh sách booth|Device registry theo tenant|Phân trang; không tải toàn bộ fleet vào browser |
| Phiên bản hiện tại|Phiên bản booth báo thực tế|Tách phiên bản được yêu cầu |
| Địa điểm/nhóm|Cấu hình chủ booth|Lưu độc lập với device identity |
| Trạng thái|Heartbeat đã chuẩn hóa|Có timestamp và bộ lọc |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Lọc/tìm|Có quyền đọc|Chỉ lọc trên tenant được phép |
| Thêm booth|Có quyền quản lý seat|Mở CUS-06 |

## Luồng chính

1. Xem danh sách.
2. Lọc booth cần quan tâm.
3. So sánh phiên bản và trạng thái.
4. Mở chi tiết một booth.

## Quy tắc nghiệp vụ đề xuất

* **CUS-04-BR-01:** Không có quyền nhóm không được thấy dữ liệu nhóm đó nếu áp dụng phân quyền theo địa điểm.
* **CUS-04-BR-02:** Lệnh hàng loạt cần bước review riêng và không là tác dụng phụ của chọn dòng.
* **CUS-04-BR-03:** Không hiển thị mọi booth của hệ thống cho khách hàng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Không có kết quả lọc|Phân biệt với chưa có booth; cho xóa bộ lọc |
| Dữ liệu cũ|Giữ dòng và đánh dấu cũ |
| Không có quyền|Không lộ tên hoặc vị trí booth khác |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-04-AC-01:** Given tenant có nhiều booth; When sang trang tiếp; Then dữ liệu được phân trang ổn định.
* **CUS-04-AC-02:** Given bộ lọc không khớp; When hiển thị; Then có nút xóa lọc.
* **CUS-04-AC-03:** Given người dùng tenant A; When sửa query ID tenant B; Then không nhận danh sách B.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-05](../decisions.md#d-05), [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
