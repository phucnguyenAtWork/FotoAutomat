# CUS-15 · Phát hành nội dung tới booth

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ tổ chức / người được quyền phát hành |
| Epic tham chiếu | EP-05 |
| Mã màn hình | CUS-15, chưa phải Jira issue key |
| Mục tiêu | Đưa bảng giá, màu và khung đã duyệt đến đúng nhóm booth và biết máy nào đã áp dụng. |

## Câu chuyện người dùng

Là chủ tổ chức / người được quyền phát hành, tôi muốn đưa bảng giá, màu và khung đã duyệt đến đúng nhóm booth và biết máy nào đã áp dụng.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-05](../screens/cus-05.md), [CUS-12](../screens/cus-12.md), [CUS-14](../screens/cus-14.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Phát hành nội dung
Chọn recipe/khung/bảng giá version
Chọn booth/nhóm đích • kiểm tra tương thích
Review thay đổi và thời điểm áp dụng
[Phát hành] → bảng theo dõi từng booth
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Manifest version|Content service|Tập version bất biến có checksum |
| Danh sách đích|Booth thuộc tenant|Không mở rộng đối tượng sau khi xác nhận mà không review |
| Trạng thái từng booth|Desired/downloaded/validated/applied|Phân biệt pending khi offline |
| Phiên đang phục vụ|Booth reported state|Thông báo áp dụng sau phiên |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Phát hành|Có quyền; nội dung hợp lệ; target tương thích|Tạo deployment có ID |
| Dừng phân phối tiếp|Có quyền|Không giả định thu hồi những gì đã áp dụng |
| Quay về version trước|Version còn tương thích|Tạo deployment mới có audit |

## Luồng chính

1. Chọn bundle và targets.
2. Xem các booth không tương thích.
3. Xác nhận phát hành.
4. Theo dõi trạng thái từng booth.

## Quy tắc nghiệp vụ đề xuất

* **CUS-15-BR-01:** Tải hết và xác minh bundle trước khi chuyển active version.
* **CUS-15-BR-02:** Không đổi recipe/giá của phiên đang trả tiền.
* **CUS-15-BR-03:** Máy offline giữ bản hợp lệ trước đó và nhận khi kết nối lại.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Một phần booth lỗi|Hiện kết quả từng máy, không báo toàn bộ thành công |
| Mất mạng khi tải|Giữ bundle cũ và có thể tiếp tục tải an toàn |
| Checksum lỗi|Không áp dụng bản lỗi |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-15-AC-01:** Given booth đang phục vụ; When bundle mới tải xong; Then đợi điểm chuyển an toàn trước áp dụng.
* **CUS-15-AC-02:** Given tải thiếu asset; When kiểm tra bundle; Then bản cũ vẫn active.
* **CUS-15-AC-03:** Given hai booth offline; When publish thành công ở server; Then hai booth đó vẫn hiển thị pending.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-05](../services/svc-05.md), [SVC-04](../services/svc-04.md).
* Quyết định còn mở: [D-09](../decisions.md#d-09), [D-13](../decisions.md#d-13).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
