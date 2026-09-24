# CUS-16 · Phiên chụp, doanh thu booth và giao file

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Chủ booth / người được cấp quyền báo cáo |
| Epic tham chiếu | EP-03 |
| Mã màn hình | CUS-16, chưa phải Jira issue key |
| Mục tiêu | Theo dõi hoạt động kinh doanh của booth và trạng thái fulfillment mà không nhầm với phí trả FotoAutomat. |

## Câu chuyện người dùng

Là chủ booth / người được cấp quyền báo cáo, tôi muốn theo dõi hoạt động kinh doanh của booth và trạng thái fulfillment mà không nhầm với phí trả FotoAutomat.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-03](../screens/cus-03.md), [CUS-05](../screens/cus-05.md), [CUS-18](../screens/cus-18.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Hoạt động booth • khoảng thời gian • địa điểm
Phiên | gói | tiền nhận | in | giao file | đồng bộ
Tổng đã đối soát • chưa đủ dữ liệu
Chi tiết phiên và lịch sử
[Xuất báo cáo theo quyền] [Yêu cầu hỗ trợ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Phiên và tiền nhận|Sự kiện booth đồng bộ|Có thời gian tại booth và thời gian server nhận |
| Trạng thái in|Bằng chứng print outcome|Unknown không được tính hoàn thành |
| Giao file|Delivery job|Không mặc định cho chủ booth mở ảnh khách |
| Tổng doanh thu|Định nghĩa báo cáo D-14|Tiền tệ, refund, thiếu dữ liệu được ghi rõ |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Xem chi tiết|Có quyền báo cáo|Hiện timeline và trạng thái đối soát |
| Xuất báo cáo|Có quyền tương ứng|Phạm vi đúng tenant/khoảng thời gian; dữ liệu nhạy cảm theo policy |

## Luồng chính

1. Chọn phạm vi báo cáo.
2. Xem mức độ đủ dữ liệu.
3. Mở các phiên cần kiểm tra.
4. Xuất hoặc gửi hỗ trợ theo quyền.

## Quy tắc nghiệp vụ đề xuất

* **CUS-16-BR-01:** Sự kiện đồng bộ lặp không tạo doanh thu trùng.
* **CUS-16-BR-02:** Phải phân biệt doanh thu khách lẻ với khoản trả cho FotoAutomat.
* **CUS-16-BR-03:** Quyền đọc metadata không mặc nhiên cấp quyền xem ảnh người chụp.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Booth chưa sync|Ghi rõ số liệu chưa đầy đủ |
| Print/delivery unknown|Có hàng cần kiểm tra |
| Không có dữ liệu trong kỳ|Empty state, không kết luận booth chưa hoạt động nếu chưa sync |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-16-AC-01:** Given cùng session được sync hai lần; When lập báo cáo; Then không cộng doanh thu hai lần.
* **CUS-16-AC-02:** Given booth chưa đồng bộ hết kỳ; When xem tổng; Then hiển thị cảnh báo dữ liệu chưa đầy đủ.
* **CUS-16-AC-03:** Given role chỉ xem báo cáo; When yêu cầu file ảnh; Then bị từ chối nếu không có quyền riêng.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-07](../services/svc-07.md), [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-04](../decisions.md#d-04), [D-10](../decisions.md#d-10), [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
