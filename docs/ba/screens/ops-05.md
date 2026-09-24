# OPS-05 · Lập đợt cập nhật và theo dõi rollout

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Release manager / vận hành được cấp quyền |
| Epic tham chiếu | EP-06 |
| Mã màn hình | OPS-05, chưa phải Jira issue key |
| Mục tiêu | Cập nhật nhiều booth theo nhóm và dừng khi có dấu hiệu lỗi. |

## Câu chuyện người dùng

Là release manager / vận hành được cấp quyền, tôi muốn cập nhật nhiều booth theo nhóm và dừng khi có dấu hiệu lỗi.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-01](../screens/ops-01.md), [OPS-04](../screens/ops-04.md), [OPS-06](../screens/ops-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Tạo rollout • release đã duyệt
Chọn tenant/booth/cohort • lịch bảo trì
Pilot → đợt kế tiếp → hoàn tất
Bảng từng booth: queued/downloading/idle wait/install/health
[Khởi chạy] [Tạm dừng] [Kế hoạch rollback]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Release đích|OPS-04|Chỉ release hợp lệ và đúng quyền nâng cấp |
| Targets snapshot|Fleet selection|Hiển thị số lượng và danh sách trước xác nhận |
| Điều kiện tiếp tục/dừng|Rollout policy|Ngưỡng và người duyệt cần D-13 |
| Kết quả từng máy|Update agent báo về|Tách reboot/install với health verified |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Khởi chạy|Target hợp lệ, có quyền, policy đủ|Tạo rollout có ID và cohort |
| Tạm dừng|Có rollout chưa hoàn tất|Dừng cấp lệnh tiếp theo; không cắt cài đặt đang ở bước nguy hiểm |
| Rollback|Bản trước và schema còn tương thích|Review rồi tạo rollout riêng |

## Luồng chính

1. Chọn release và nhóm thử nghiệm.
2. Xem lịch và điều kiện dừng.
3. Triển khai thử, đánh giá sức khỏe.
4. Mở rộng hoặc dừng theo policy và quyền.

## Quy tắc nghiệp vụ đề xuất

* **OPS-05-BR-01:** Không cập nhật giữa phiên trả tiền.
* **OPS-05-BR-02:** Máy offline cần trạng thái chờ và hạn lệnh rõ.
* **OPS-05-BR-03:** Không coi tải xong là đã cài; không coi đã cài là đã khỏe.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Lỗi tăng trong cohort|Dừng mở rộng và hiển thị nguyên nhân |
| Booth đang bận|Wait-for-idle thay vì restart ép |
| Rollback không an toàn|Chặn và đưa kế hoạch khôi phục thủ công |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-05-AC-01:** Given booth đang có phiên trả tiền; When nhận update; Then hoãn cài đến điểm an toàn.
* **OPS-05-AC-02:** Given cohort chưa đạt health gate; When mở rộng; Then bị chặn theo policy.
* **OPS-05-AC-03:** Given rollback không tương thích schema; When yêu cầu; Then hệ thống từ chối tự động hạ bản.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-04](../services/svc-04.md), [SVC-06](../services/svc-06.md).
* Quyết định còn mở: [D-03](../decisions.md#d-03), [D-13](../decisions.md#d-13), [D-14](../decisions.md#d-14).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
