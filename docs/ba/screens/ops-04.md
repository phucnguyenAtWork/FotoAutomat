# OPS-04 · Danh mục phiên bản và bộ cài

Trạng thái: **BA draft v0.1**, chưa phải thiết kế đã duyệt hay tính năng đã triển khai.

| Thuộc tính | Giá trị |
| --- | --- |
| Khu vực | Quản trị nội bộ FotoAutomat |
| Vai trò đề xuất | Release manager |
| Epic tham chiếu | EP-06 |
| Mã màn hình | OPS-04, chưa phải Jira issue key |
| Mục tiêu | Đăng ký bản phần mềm hợp lệ và điều kiện phân phối trước khi triển khai. |

## Câu chuyện người dùng

Là release manager, tôi muốn đăng ký bản phần mềm hợp lệ và điều kiện phân phối trước khi triển khai.

## Điểm vào và điều hướng

Các màn hình liên quan: [OPS-05](../screens/ops-05.md), [CUS-06](../screens/cus-06.md). Điều hướng phải giữ đúng phiên/tenant và theo quyền, không dựa vào việc người dùng biết URL.

## Bố cục sơ bộ

Đây là wireframe nội dung, chưa chốt phong cách thị giác, pixel, font hoặc artwork.

```text
Phiên bản phần mềm | [Đăng ký release]
Version • kênh • Windows/architecture hỗ trợ
Artifact • hash • bằng chứng chữ ký
Schema compatibility • release notes • rollback
[Đánh dấu đủ điều kiện] [Thu hồi phân phối]
```

## Dữ liệu và thành phần UI

| Thành phần | Nguồn nghiệp vụ | Ràng buộc UI |
| --- | --- | --- |
| Artifact và manifest|Build/release pipeline|Bất biến theo release ID; không thay file cùng version |
| Chữ ký/hash|Quy trình ký và kiểm tra|Không chấp nhận chỉ tick checkbox thủ công làm bằng chứng |
| Tương thích/rollback|Metadata đã kiểm thử|Gồm schema local và renderer recipe |

## Hành động

| Hành động | Điều kiện | Kết quả |
| --- | --- | --- |
| Đăng ký release|Có quyền release|Upload/đăng ký artifact chưa phát hành |
| Cho phép rollout|Qua kiểm tra quy định|Release vào danh sách chọn của OPS-05 |
| Thu hồi phân phối|Có lý do và quyền|Chặn cấp mới; không tự gỡ app đang chạy |

## Luồng chính

1. Nhận artifact từ pipeline.
2. Gắn bằng chứng và notes.
3. Kiểm tra tương thích và rollback.
4. Cho phép release được chọn trong rollout.

## Quy tắc nghiệp vụ đề xuất

* **OPS-04-BR-01:** Bộ cài thương mại cần quy trình ký, không dùng thẳng bundle CI simulator.
* **OPS-04-BR-02:** Chữ ký sai hoặc artifact bị thay đổi không được phát hành.
* **OPS-04-BR-03:** Hạ version chỉ được phép nếu dữ liệu còn tương thích.

## Trạng thái và ngoại lệ

Áp dụng thêm [quy tắc UX dùng chung](../ux-principles.md), đặc biệt loading, validation, quyền truy cập, dữ liệu cũ và chống thao tác lặp.

| Tình huống | Hành vi UI/UX |
| --- | --- |
| Artifact thiếu/chữ ký lỗi|Chặn release |
| Không có kế hoạch schema rollback|Đánh dấu hạn chế rollback rõ |
| Release bị thu hồi|Mọi rollout chưa bắt đầu phải kiểm tra lại |

## Tiêu chí nghiệm thu

Các tiêu chí dưới đây là mục tiêu cho thiết kế và triển khai sau khi quyết định liên quan được duyệt.

* **OPS-04-AC-01:** Given chữ ký không hợp lệ; When yêu cầu phát hành; Then release không đủ điều kiện.
* **OPS-04-AC-02:** Given một version đã đăng ký; When thay artifact; Then phải tạo release mới hoặc bị từ chối.
* **OPS-04-AC-03:** Given schema không hỗ trợ downgrade; When xem release; Then UI không hứa rollback tự động.

## Phụ thuộc và điểm cần quyết định

* Năng lực hệ thống: [SVC-06](../services/svc-06.md), [SVC-10](../services/svc-10.md).
* Quyết định còn mở: [D-11](../decisions.md#d-11), [D-13](../decisions.md#d-13).
* Khi quyết định ảnh hưởng đến tiền, license, retention hoặc side effect chưa được duyệt, chỉ thiết kế trạng thái; chưa coi story đủ điều kiện triển khai thương mại.

## Bàn giao UI/UX

Bản thiết kế cần thể hiện bố cục, nội dung CTA, trạng thái tương ứng ở trên, đường đi tới màn hình liên quan, và đánh dấu rõ mọi giả định. BA này không tự cấp quyền truy cập ảnh, chốt giá hoặc xác định chính sách pháp lý.
