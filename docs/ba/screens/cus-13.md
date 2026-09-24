# CUS-13 · Tạo màu bằng AI và tinh chỉnh

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Cổng khách hàng thương mại |
| Vai trò đề xuất | Người thiết kế nội dung được cấp quyền |
| Epic tham chiếu | EP-05 |
| Mã màn hình | CUS-13, chưa phải Jira issue key |
| Mục tiêu | Tạo gợi ý màu từ ảnh tham khảo, thử trên ảnh mẫu và duyệt công thức có thể chạy tại booth. |

## Câu chuyện người dùng

Là người thiết kế nội dung được cấp quyền, tôi muốn tạo gợi ý màu từ ảnh tham khảo, thử trên ảnh mẫu và duyệt công thức có thể chạy tại booth.

## Điểm vào và điều hướng

Các màn hình liên quan: [CUS-10](../screens/cus-10.md), [CUS-12](../screens/cus-12.md), [CUS-15](../screens/cus-15.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Studio màu
Ảnh tham khảo [tải lên] | ảnh thử [ ]
[Tạo gợi ý AI] • chi phí/quota dự kiến
[Trước / sau] • thông số màu chỉnh tay
[Lưu nháp] [Đánh dấu sẵn sàng phát hành]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Ảnh tham khảo|Người dùng tải lên|Giới hạn file/định dạng/quyền sử dụng được cấu hình |
| AI job|Job service|Có ID, tiến trình theo bước và chi phí rõ |
| Thông số recipe|Kết quả AI + chỉnh tay|Kiểm tra schema, không chứa mã thực thi tùy ý |
| Ảnh thử|Asset thử đã chọn|Có phiên bản renderer cho so sánh |
| Quota dự kiến|Service policy|Cảnh báo trước khi gửi job có phí |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Tạo gợi ý AI|File hợp lệ, có quyền/quota|Tạo một job có định danh |
| Lưu nháp|Recipe hợp lệ|Lưu version và provenance |
| Sẵn sàng phát hành|Đã preview, kiểm tra khả năng local|Đánh dấu duyệt, không tự đẩy xuống booth |

## Luồng chính

1. Tải tham khảo và chọn ảnh thử.
2. Xem chi phí rồi tạo gợi ý.
3. Chờ job, xem trước/sau và chỉnh thông số.
4. Lưu bản nháp hoặc duyệt để phân phối.

## Quy tắc nghiệp vụ đề xuất

* **CUS-13-BR-01:** AI chạy ở phía quản trị/cloud theo định hướng, không là điều kiện Internet cho phiên chụp tại booth.
* **CUS-13-BR-02:** Kết quả trả về phải chuyển thành recipe được renderer local hỗ trợ.
* **CUS-13-BR-03:** Retry job sau timeout không được tự thu phí lần hai; hủy/refund usage theo D-09.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Quota hết|Không gửi job có phí; đưa lựa chọn mua thêm |
| Job lỗi/timeout|Cho xem trạng thái và retry có kiểm soát |
| Kết quả không tương thích|Không cho duyệt dù ảnh preview cloud trông hợp lệ |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **CUS-13-AC-01:** Given bấm tạo AI lặp do timeout; When gửi cùng job request; Then không phát sinh job tính phí trùng.
* **CUS-13-AC-02:** Given recipe chứa thành phần renderer local không hỗ trợ; When duyệt; Then bị chặn và nêu thành phần lỗi.
* **CUS-13-AC-03:** Given lưu nháp; When chưa publish; Then các booth đang phục vụ không đổi màu.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-08](../services/svc-08.md), [SVC-05](../services/svc-05.md).
* Quyết định còn mở: [D-09](../decisions.md#d-09), [D-10](../decisions.md#d-10), [D-11](../decisions.md#d-11).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
