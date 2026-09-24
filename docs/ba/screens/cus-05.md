# CUS-05 · Chi tiết booth và tác vụ từ xa

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ booth / vận hành được cấp quyền |
| Epic tham chiếu | EP-03 |
| Mã màn hình | CUS-05, chưa phải Jira issue key |
| Mục tiêu | Xem cấu hình thực tế và yêu cầu thao tác từ xa có theo dõi kết quả. |

## Câu chuyện người dùng

Là chủ booth / vận hành được cấp quyền, tôi muốn xem cấu hình thực tế và yêu cầu thao tác từ xa có theo dõi kết quả.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-04](../screens/cus-04.md), [CUS-15](../screens/cus-15.md), [CUS-18](../screens/cus-18.md), [OPS-06](../screens/ops-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Tên booth • địa điểm • liên lạc cuối
Tab: Tổng quan | Thiết bị | Nội dung | Phiên bản | Tác vụ
Đang áp dụng [ ] • mong muốn [ ]
Phiên đang chạy • lỗi • quyền
[Yêu cầu đồng bộ] [Đặt lịch cập nhật] [Xem hỗ trợ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Thiết bị và sức khỏe|Booth telemetry|Có thời điểm đo; phần cứng không báo thì ghi không có dữ liệu |
| Desired/reported version|Máy chủ và booth|Hiện hai giá trị riêng |
| Trạng thái lệnh|Command service|Queued/received/running/succeeded/failed/expired/unknown |
| Hoạt động phiên|Telemetry đã rút gọn|Không mặc định hiển thị ảnh khách |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Yêu cầu đồng bộ|Có quyền và target xác định|Tạo command ID có hạn |
| Đặt lịch cập nhật|Bản tương thích, có quyền cập nhật|Tạo kế hoạch; không báo đã cài ngay |
| Thu hồi/chuyển booth|Có quyền đặc biệt và policy|Yêu cầu xác nhận tác động trước thực hiện |

## Luồng chính

1. Xem last seen và phiên bản thực tế.
2. Chọn tác vụ được phép.
3. Review booth đích và điều kiện thực thi.
4. Theo dõi acknowledgement và kết quả.

## Quy tắc nghiệp vụ đề xuất

* **CUS-05-BR-01:** Chấp nhận lệnh ở server không đồng nghĩa booth đã thực hiện.
* **CUS-05-BR-02:** Không restart/cập nhật giữa phiên đã trả tiền.
* **CUS-05-BR-03:** Không có chức năng chạy shell tùy ý trong BA này.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Booth offline|Cho xếp lệnh nếu hợp lệ; hiển thị đang chờ kết nối |
| Lệnh hết hạn|Không thực thi muộn khi máy trở lại |
| Bản cập nhật không tương thích|Chặn chọn và giải thích |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-05-AC-01:** Given booth offline; When gửi yêu cầu sync; Then UI hiện queued, không succeeded.
* **CUS-05-AC-02:** Given có phiên trả tiền; When đến lịch update; Then booth hoãn đến thời điểm an toàn.
* **CUS-05-AC-03:** Given command đã quá hạn; When booth kết nối lại; Then không chạy lệnh đó.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-05](../services/svc-05.md), [SVC-06](../services/svc-06.md).
* Quyết định còn mở: [D-02](../decisions.md#d-02), [D-13](../decisions.md#d-13), [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
