# BTH-02 · Cấu hình và kiểm tra thiết bị

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Ứng dụng booth Windows |
| Vai trò đề xuất | Kỹ thuật viên / vận hành booth |
| Epic tham chiếu | EP-01 |
| Mã màn hình | BTH-02, chưa phải Jira issue key |
| Mục tiêu | Chọn camera, máy in, bộ nhận tiền và xác minh khả năng phục vụ trước khi mở bán. |

## Câu chuyện người dùng

Là kỹ thuật viên / vận hành booth, tôi muốn chọn camera, máy in, bộ nhận tiền và xác minh khả năng phục vụ trước khi mở bán.

## Điểm vào và điều hướng

Các màn hình liên quan: [BTH-01](../screens/bth-01.md), [BTH-03](../screens/bth-03.md), [BTH-12](../screens/bth-12.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Thiết lập booth | Khóa chế độ khách
Camera [thiết bị]  [Xem trước] [Chụp thử]
Máy in [queue] • khổ giấy [ ] [In thử]
Bộ nhận tiền [adapter/cổng] [Kiểm tra]
Kết quả kiểm tra • lỗi • hướng khắc phục
[Lưu cấu hình] [Mở booth]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Thiết bị/cổng/queue|Thiết bị được máy Windows phát hiện|Không tự chọn phần cứng khác khi thiết bị cũ mất kết nối |
| Khổ in và cấu hình màu|Driver đã kiểm chứng + cấu hình booth|Chỉ hiện tổ hợp được hỗ trợ |
| Kết quả kiểm tra|Phản hồi adapter tại máy|Có thời điểm, phân biệt pass/fail/chưa kiểm tra |
| Phiên bản SDK/driver|Adapter chẩn đoán|Thông tin kỹ thuật chỉ ở chế độ vận hành |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Chụp thử / in thử|Đã vào chế độ vận hành và xác nhận thao tác thật|Tạo ảnh/test job riêng, không tính là doanh thu |
| Mở booth|Các điều kiện sẵn sàng đạt và không có phiên cần giải quyết|Chuyển sang BTH-03 |

## Luồng chính

1. Chọn đúng các thiết bị kết nối.
2. Kiểm tra camera rồi cấu hình khổ in.
3. Xác nhận trước khi in thử hoặc kiểm thử bộ nhận tiền.
4. Lưu kết quả và mở chế độ khách khi đủ điều kiện.

## Quy tắc nghiệp vụ đề xuất

* **BTH-02-BR-01:** Khách chụp không được truy cập cấu hình.
* **BTH-02-BR-02:** Không thay đổi thiết bị giữa phiên đã trả tiền.
* **BTH-02-BR-03:** Kết quả simulator không đủ để đánh dấu phần cứng thật sẵn sàng.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Camera/USB mất kết nối|Giữ lựa chọn; cho kết nối lại; chặn mở bán nếu bắt buộc |
| In thử không rõ kết quả|Đánh dấu cần kiểm tra; không tự in lần hai |
| Offline|Cho kiểm tra thiết bị cục bộ; trạng thái đồng bộ cấu hình là đang chờ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **BTH-02-AC-01:** Given camera không sẵn sàng; When chọn mở booth; Then UI giải thích điều kiện bị thiếu và không cho nhận tiền.
* **BTH-02-AC-02:** Given có phiên trả tiền đang chạy; When đổi queue máy in; Then thao tác bị chặn đến khi kết thúc hoặc xử lý phiên.
* **BTH-02-AC-03:** Given test in đã gửi nhưng chưa rõ kết quả; When mở lại trang; Then trạng thái giữ là cần kiểm tra, không tự gửi lại.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-09](../services/svc-09.md).
* Quyết định còn mở: [D-07](../decisions.md#d-07), [D-08](../decisions.md#d-08), [D-11](../decisions.md#d-11).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
