# BTH-10 · Tiến trình in và lỗi in

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Khách chụp ảnh |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-10, chưa phải Jira issue key |
| Mục tiêu | Cho khách biết đang xử lý, đã gửi hay đã xác nhận in xong; hỗ trợ khi không rõ kết quả. |

## Câu chuyện người dùng

Là khách chụp ảnh, tôi muốn cho khách biết đang xử lý, đã gửi hay đã xác nhận in xong; hỗ trợ khi không rõ kết quả.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-09](../screens/bth-09.md), [BTH-11](../screens/bth-11.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Đang chuẩn bị / đang in
[Tiến trình theo các bước có bằng chứng]
Số bản đã xác nhận • hướng dẫn lấy ảnh
Mã phiên hỗ trợ
[Nhận file] hoặc [Gọi hỗ trợ]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Job ID|Print intent local|Ổn định qua restart |
| Trạng thái|Adapter, spooler và bằng chứng thiết bị|Phân biệt submitted/completed/unknown |
| Số bản hoàn thành|Bằng chứng đáng tin cậy nếu có|Không giả lập tiến trình 100% theo timer |
| Lỗi vật tư|Thiết bị nếu báo được|Ngôn ngữ dễ hiểu, không dump stack trace |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Nhận file|Digital artifact đã có; in có trạng thái phù hợp|Mở BTH-11 và vẫn giữ trạng thái in thật |
| Gọi hỗ trợ|Lỗi/unknown|Hiện mã phiên; đưa incident vào BTH-12 |

## Luồng chính

1. Hiện chuẩn bị rồi gửi job.
2. Cập nhật theo bằng chứng nhận được.
3. Hướng dẫn lấy ảnh khi đã có cơ sở xác nhận.
4. Chuyển nhận file hoặc hỗ trợ.

## Quy tắc nghiệp vụ đề xuất

* **BTH-10-BR-01:** Spooler nhận job không đồng nghĩa giấy đã ra.
* **BTH-10-BR-02:** Timeout/restart sau submit không được kích hoạt tự in lại.
* **BTH-10-BR-03:** Quy tắc kết thúc khi driver không có tín hiệu hoàn thành là D-08, chưa được tự chốt.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Hết giấy/ribbon|Hiện hỗ trợ; giữ job để đối soát |
| Không rõ kết quả|Thông báo cần kiểm tra; không có nút tự in lại cho khách |
| Mất mạng|In local tiếp tục; upload file là trạng thái riêng |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-10-AC-01:** Given chỉ có xác nhận từ spooler; When cập nhật màn hình; Then không gắn nhãn giấy đã in xong.
* **BTH-10-AC-02:** Given restart sau khi gửi lệnh; When phục hồi; Then job giữ trạng thái cần kiểm tra và không được submit lại tự động.
* **BTH-10-AC-03:** Given Internet mất nhưng printer vẫn hoạt động; When in; Then luồng in không phụ thuộc cloud.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-08](../decisions.md#d-08).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
