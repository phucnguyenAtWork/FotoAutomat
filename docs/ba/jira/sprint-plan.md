# Kế hoạch sprint FotoAutomat · Ba giai đoạn toàn dự án

Ngày: 2026-09-25. Trạng thái: đã tạo 11 sprint tương lai và phân bổ 66 Story/Task trên Jira; ngày, ước lượng và người phụ trách chưa chốt.

## Nguyên tắc

Theo lựa chọn của chủ sản phẩm: hoàn thành toàn bộ UI/UX → Backend và tích hợp → Test, CI/CD. UI/UX bao gồm giao diện chạy với dữ liệu giả; Backend bao gồm nối giao diện, nghiệp vụ local, cloud, dữ liệu và thiết bị. Không bỏ sót phần lập trình giao diện.

11 sprint bên dưới là các nhóm mục tiêu dự kiến, chưa phải cam kết hoàn thành trong 22 tuần. Nhân lực, năng suất, phần cứng và chính sách chưa chốt; phải ước lượng và tách sprint quá tải trước khi bắt đầu. Các sprint tương lai chưa có ngày bắt đầu/kết thúc. Sprint 0 đang active được giữ nguyên.

7 Epic quản lý phạm vi xuyên sprint. 50 Story/Task trước đó nằm trong sprint tích hợp; 7 Task UI/UX, 7 Task QA và 2 Task CI/CD được tạo thêm. Tổng: 73 mục, gồm 7 Epic và 66 Story/Task. [Bản ghi Jira và kết quả kiểm tra](sprint-published.json).

Kiểm tra build và test hiện có vẫn chạy khi phát triển. Giai đoạn cuối tập trung kiểm thử hệ thống, hồi quy, hiệu năng và hoàn thiện CI/CD; không xóa hoặc trì hoãn các kiểm tra đang bảo vệ repo.

## Mục tiêu và điều kiện bàn giao

| Phần | Mục tiêu | Điều kiện hoàn thành |
| --- | --- | --- |
| UI/UX · FA-01–02 | Chốt trải nghiệm của 40 màn hình trước khi tích hợp | UI dữ liệu giả, luồng và trạng thái ngoại lệ, quyền, hợp đồng dữ liệu; chủ sản phẩm duyệt, quyết định mở ghi rõ |
| Backend · FA-03–09 | Biến UI đã duyệt thành chức năng với dữ liệu và thiết bị | UI nối dịch vụ, API/schema được tài liệu hóa, nghiệp vụ offline/quyền/đối soát đúng; bằng chứng kiểm tra từng chức năng |
| Test, CI/CD · FA-10–11 | Kiểm chứng hệ thống và quy trình phát hành | Báo cáo hồi quy/bảo mật/hiệu năng/Windows/hardware; pipeline có bằng chứng, diễn tập rollback, tiêu chí pilot được duyệt |

Không chuyển giai đoạn khi các đầu ra bắt buộc chưa được duyệt. Backend không gọi dịch vụ SDK/thiết bị là đã tích hợp khi mới có simulator. Tiêu chí tải/hiệu năng, retention và billing phải lấy từ quyết định đã duyệt, không tự đặt giá trị.

## Các sprint

| Sprint | Mục tiêu | Ticket đã có | Task bổ sung |
| --- | --- | --- | --- |
| FA-01 · UI/UX · Booth, tài khoản và giao ảnh | Duyệt luồng và xây dựng giao diện dữ liệu giả cho kích hoạt, phiên chụp offline và nhận ảnh qua QR. |  | [SCRUM-62](https://phucnguyen31work.atlassian.net/browse/SCRUM-62) · UX-01, [SCRUM-63](https://phucnguyen31work.atlassian.net/browse/SCRUM-63) · UX-02, [SCRUM-68](https://phucnguyen31work.atlassian.net/browse/SCRUM-68) · UX-07 |
| FA-02 · UI/UX · Cổng thương mại và vận hành | Hoàn thành 40 màn hình; duyệt trạng thái, quyền, dữ liệu và hợp đồng tích hợp để bàn giao Backend. |  | [SCRUM-64](https://phucnguyen31work.atlassian.net/browse/SCRUM-64) · UX-03, [SCRUM-65](https://phucnguyen31work.atlassian.net/browse/SCRUM-65) · UX-04, [SCRUM-66](https://phucnguyen31work.atlassian.net/browse/SCRUM-66) · UX-05, [SCRUM-67](https://phucnguyen31work.atlassian.net/browse/SCRUM-67) · UX-06 |
| FA-03 · Backend · Tài khoản và cách ly dữ liệu | Tích hợp đăng nhập, tổ chức, phân quyền và audit để dữ liệu chỉ được truy cập đúng phạm vi. | [SCRUM-24](https://phucnguyen31work.atlassian.net/browse/SCRUM-24), [SCRUM-25](https://phucnguyen31work.atlassian.net/browse/SCRUM-25), [SCRUM-40](https://phucnguyen31work.atlassian.net/browse/SCRUM-40), [SCRUM-49](https://phucnguyen31work.atlassian.net/browse/SCRUM-49), [SCRUM-61](https://phucnguyen31work.atlassian.net/browse/SCRUM-61) |  |
| FA-04 · Backend · Giấy phép, thanh toán và kích hoạt | Cấp đúng quyền thuê/vĩnh viễn; đối soát thanh toán và kích hoạt booth không trùng seat. | [SCRUM-12](https://phucnguyen31work.atlassian.net/browse/SCRUM-12), [SCRUM-29](https://phucnguyen31work.atlassian.net/browse/SCRUM-29), [SCRUM-30](https://phucnguyen31work.atlassian.net/browse/SCRUM-30), [SCRUM-31](https://phucnguyen31work.atlassian.net/browse/SCRUM-31), [SCRUM-32](https://phucnguyen31work.atlassian.net/browse/SCRUM-32), [SCRUM-33](https://phucnguyen31work.atlassian.net/browse/SCRUM-33), [SCRUM-34](https://phucnguyen31work.atlassian.net/browse/SCRUM-34), [SCRUM-43](https://phucnguyen31work.atlassian.net/browse/SCRUM-43), [SCRUM-44](https://phucnguyen31work.atlassian.net/browse/SCRUM-44), [SCRUM-48](https://phucnguyen31work.atlassian.net/browse/SCRUM-48), [SCRUM-52](https://phucnguyen31work.atlassian.net/browse/SCRUM-52), [SCRUM-53](https://phucnguyen31work.atlassian.net/browse/SCRUM-53), [SCRUM-54](https://phucnguyen31work.atlassian.net/browse/SCRUM-54) |  |
| FA-05 · Backend · Phiên chụp và thiết bị tại booth | Hoàn thành nhận tiền, chụp, xử lý, in offline và phục hồi phiên; không tự in lại khi kết quả chưa xác định. | [SCRUM-13](https://phucnguyen31work.atlassian.net/browse/SCRUM-13), [SCRUM-14](https://phucnguyen31work.atlassian.net/browse/SCRUM-14), [SCRUM-15](https://phucnguyen31work.atlassian.net/browse/SCRUM-15), [SCRUM-16](https://phucnguyen31work.atlassian.net/browse/SCRUM-16), [SCRUM-17](https://phucnguyen31work.atlassian.net/browse/SCRUM-17), [SCRUM-18](https://phucnguyen31work.atlassian.net/browse/SCRUM-18), [SCRUM-19](https://phucnguyen31work.atlassian.net/browse/SCRUM-19), [SCRUM-20](https://phucnguyen31work.atlassian.net/browse/SCRUM-20), [SCRUM-21](https://phucnguyen31work.atlassian.net/browse/SCRUM-21), [SCRUM-23](https://phucnguyen31work.atlassian.net/browse/SCRUM-23), [SCRUM-60](https://phucnguyen31work.atlassian.net/browse/SCRUM-60) |  |
| FA-06 · Backend · AI, công thức màu và nội dung | Tạo, duyệt và đồng bộ công thức màu/khung có phiên bản; ghi nhận usage dịch vụ AI. | [SCRUM-35](https://phucnguyen31work.atlassian.net/browse/SCRUM-35), [SCRUM-36](https://phucnguyen31work.atlassian.net/browse/SCRUM-36), [SCRUM-37](https://phucnguyen31work.atlassian.net/browse/SCRUM-37), [SCRUM-38](https://phucnguyen31work.atlassian.net/browse/SCRUM-38), [SCRUM-56](https://phucnguyen31work.atlassian.net/browse/SCRUM-56), [SCRUM-59](https://phucnguyen31work.atlassian.net/browse/SCRUM-59) |  |
| FA-07 · Backend · Giao ảnh qua QR | Xếp hàng upload khi offline và giao đúng ảnh cho người có quyền; xử lý link hết hạn hoặc bị thu hồi. | [SCRUM-22](https://phucnguyen31work.atlassian.net/browse/SCRUM-22), [SCRUM-50](https://phucnguyen31work.atlassian.net/browse/SCRUM-50), [SCRUM-51](https://phucnguyen31work.atlassian.net/browse/SCRUM-51), [SCRUM-58](https://phucnguyen31work.atlassian.net/browse/SCRUM-58) |  |
| FA-08 · Backend · Giám sát fleet và hỗ trợ | Máy chủ nắm trạng thái booth, phiên chụp và lệnh từ xa; phân biệt dữ liệu cũ và kết quả chưa xác định. | [SCRUM-26](https://phucnguyen31work.atlassian.net/browse/SCRUM-26), [SCRUM-27](https://phucnguyen31work.atlassian.net/browse/SCRUM-27), [SCRUM-28](https://phucnguyen31work.atlassian.net/browse/SCRUM-28), [SCRUM-39](https://phucnguyen31work.atlassian.net/browse/SCRUM-39), [SCRUM-41](https://phucnguyen31work.atlassian.net/browse/SCRUM-41), [SCRUM-42](https://phucnguyen31work.atlassian.net/browse/SCRUM-42), [SCRUM-47](https://phucnguyen31work.atlassian.net/browse/SCRUM-47), [SCRUM-55](https://phucnguyen31work.atlassian.net/browse/SCRUM-55) |  |
| FA-09 · Backend · Phân phối phiên bản và cập nhật | Tích hợp release, rollout theo nhóm và rollback tương thích; không gián đoạn phiên trả tiền. | [SCRUM-45](https://phucnguyen31work.atlassian.net/browse/SCRUM-45), [SCRUM-46](https://phucnguyen31work.atlassian.net/browse/SCRUM-46), [SCRUM-57](https://phucnguyen31work.atlassian.net/browse/SCRUM-57) |  |
| FA-10 · Test, CI/CD · Hồi quy và chất lượng | Kiểm chứng luồng khách hàng, tiền/quyền, offline, cách ly dữ liệu và pipeline build/test có bằng chứng. |  | [SCRUM-69](https://phucnguyen31work.atlassian.net/browse/SCRUM-69) · QA-01, [SCRUM-70](https://phucnguyen31work.atlassian.net/browse/SCRUM-70) · QA-02, [SCRUM-72](https://phucnguyen31work.atlassian.net/browse/SCRUM-72) · QA-04, [SCRUM-73](https://phucnguyen31work.atlassian.net/browse/SCRUM-73) · QA-05, [SCRUM-75](https://phucnguyen31work.atlassian.net/browse/SCRUM-75) · QA-07, [SCRUM-76](https://phucnguyen31work.atlassian.net/browse/SCRUM-76) · CICD-01 |
| FA-11 · Test, CI/CD · Fleet và phát hành thử nghiệm | Kiểm chứng giám sát nhiều booth, tải mục tiêu, cập nhật, phục hồi và phát hành có kiểm soát trước pilot. |  | [SCRUM-71](https://phucnguyen31work.atlassian.net/browse/SCRUM-71) · QA-03, [SCRUM-74](https://phucnguyen31work.atlassian.net/browse/SCRUM-74) · QA-06, [SCRUM-77](https://phucnguyen31work.atlassian.net/browse/SCRUM-77) · CICD-02 |

## Phụ thuộc cần xử lý

- Trước FA-01: chọn công nghệ giao diện web, quy tắc thiết kế và thiết bị đích đủ để dựng UI; đây là quyết định còn mở, không đổi lựa chọn WPF hiện có.
- Trước FA-03: duyệt UI cả hai sprint, chọn công nghệ server và hợp đồng API/schema; hoàn thiện chính sách phân quyền.
- Trước FA-04: chốt mô hình quyền, payment provider, offline grace và seat; không nhận tiền thật khi chỉ có sandbox.
- Trước FA-05: có hardware proof và phần cứng được cho phép thử; dùng nội dung mẫu có phiên bản, thay bằng nội dung thật ở FA-06.
- Trước FA-06–07: chốt quyền ảnh, retention, AI/quota và công thức; kiểm tra lại tích hợp booth sau đồng bộ/giao ảnh.
- Trước FA-08–09: chốt định nghĩa offline/stale, danh mục lệnh và chính sách cập nhật; bản cài đánh giá ở FA-04 khác bộ cài production được ký ở FA-11.
- Trước FA-10: tất cả chức năng của FA-03–09 có đầu ra tích hợp; các case chờ thiết bị/chính sách không được tính là đạt.
- Trước pilot: hoàn thành FA-11 và duyệt vận hành; lập kế hoạch không đồng nghĩa cho phép triển khai production.

## Phạm vi của 7 Epic

| Epic | UI/UX | Backend | Test |
| --- | --- | --- | --- |
| [SCRUM-5](https://phucnguyen31work.atlassian.net/browse/SCRUM-5) · Vận hành phiên chụp tại booth khi mất mạng | [SCRUM-62](https://phucnguyen31work.atlassian.net/browse/SCRUM-62) · UX-01 | FA-05 | [SCRUM-69](https://phucnguyen31work.atlassian.net/browse/SCRUM-69) · QA-01 |
| [SCRUM-6](https://phucnguyen31work.atlassian.net/browse/SCRUM-6) · Quản lý tài khoản và kích hoạt thiết bị theo tổ chức | [SCRUM-63](https://phucnguyen31work.atlassian.net/browse/SCRUM-63) · UX-02 | FA-03, FA-04 | [SCRUM-70](https://phucnguyen31work.atlassian.net/browse/SCRUM-70) · QA-02 |
| [SCRUM-7](https://phucnguyen31work.atlassian.net/browse/SCRUM-7) · Giám sát booth và xử lý sự cố từ xa | [SCRUM-64](https://phucnguyen31work.atlassian.net/browse/SCRUM-64) · UX-03 | FA-08 | [SCRUM-71](https://phucnguyen31work.atlassian.net/browse/SCRUM-71) · QA-03 |
| [SCRUM-8](https://phucnguyen31work.atlassian.net/browse/SCRUM-8) · Kinh doanh bản quyền và dịch vụ trả phí | [SCRUM-65](https://phucnguyen31work.atlassian.net/browse/SCRUM-65) · UX-04 | FA-04 | [SCRUM-72](https://phucnguyen31work.atlassian.net/browse/SCRUM-72) · QA-04 |
| [SCRUM-9](https://phucnguyen31work.atlassian.net/browse/SCRUM-9) · Tạo và phân phối công thức màu, khung ảnh | [SCRUM-66](https://phucnguyen31work.atlassian.net/browse/SCRUM-66) · UX-05 | FA-06 | [SCRUM-73](https://phucnguyen31work.atlassian.net/browse/SCRUM-73) · QA-05 |
| [SCRUM-10](https://phucnguyen31work.atlassian.net/browse/SCRUM-10) · Phân phối và cập nhật phần mềm booth có kiểm soát | [SCRUM-67](https://phucnguyen31work.atlassian.net/browse/SCRUM-67) · UX-06 | FA-09 | [SCRUM-74](https://phucnguyen31work.atlassian.net/browse/SCRUM-74) · QA-06 |
| [SCRUM-11](https://phucnguyen31work.atlassian.net/browse/SCRUM-11) · Giao ảnh qua QR và bảo vệ quyền truy cập | [SCRUM-68](https://phucnguyen31work.atlassian.net/browse/SCRUM-68) · UX-07 | FA-07 | [SCRUM-75](https://phucnguyen31work.atlassian.net/browse/SCRUM-75) · QA-07 |

## 16 Task theo dõi giai đoạn đã tạo trên Jira

### [SCRUM-62](https://phucnguyen31work.atlassian.net/browse/SCRUM-62) · UX-01 · Thiết kế và dựng UI booth để kiểm chứng trọn phiên chụp

Epic: SCRUM-5. Sprint: FA-01.

Mục tiêu: Thiết kế và dựng UI booth để kiểm chứng trọn phiên chụp.

Cần làm: Mô phỏng nhận tiền, live view, chọn ảnh, làm đẹp, khung, in và phục hồi; ưu tiên cảm ứng và máy cấu hình thấp.

Phạm vi: SCRUM-13 (BTH-02), SCRUM-14 (BTH-03), SCRUM-15 (BTH-04), SCRUM-16 (BTH-05), SCRUM-17 (BTH-06), SCRUM-18 (BTH-07), SCRUM-19 (BTH-08), SCRUM-20 (BTH-09), SCRUM-21 (BTH-10), SCRUM-23 (BTH-12).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-01.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-62](https://phucnguyen31work.atlassian.net/browse/SCRUM-62) · UX-01.

### [SCRUM-63](https://phucnguyen31work.atlassian.net/browse/SCRUM-63) · UX-02 · Thiết kế UI tài khoản và kích hoạt để làm rõ quyền truy cập

Epic: SCRUM-6. Sprint: FA-01.

Mục tiêu: Thiết kế UI tài khoản và kích hoạt để làm rõ quyền truy cập.

Cần làm: Dựng đăng nhập, tổ chức, thành viên, kích hoạt, tải bộ cài và audit; phân biệt vai trò khách hàng với staff.

Phạm vi: SCRUM-12 (BTH-01), SCRUM-24 (CUS-01), SCRUM-25 (CUS-02), SCRUM-29 (CUS-06), SCRUM-40 (CUS-17), SCRUM-49 (OPS-08).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-01.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-63](https://phucnguyen31work.atlassian.net/browse/SCRUM-63) · UX-02.

### [SCRUM-64](https://phucnguyen31work.atlassian.net/browse/SCRUM-64) · UX-03 · Thiết kế UI giám sát để nhận biết booth cần xử lý

Epic: SCRUM-7. Sprint: FA-02.

Mục tiêu: Thiết kế UI giám sát để nhận biết booth cần xử lý.

Cần làm: Dựng dashboard, danh sách/chi tiết booth, phiên chụp và hỗ trợ; hiển thị last seen, stale và lệnh chưa rõ kết quả.

Phạm vi: SCRUM-26 (CUS-03), SCRUM-27 (CUS-04), SCRUM-28 (CUS-05), SCRUM-39 (CUS-16), SCRUM-41 (CUS-18), SCRUM-42 (OPS-01), SCRUM-47 (OPS-06).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-02.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-64](https://phucnguyen31work.atlassian.net/browse/SCRUM-64) · UX-03.

### [SCRUM-65](https://phucnguyen31work.atlassian.net/browse/SCRUM-65) · UX-04 · Thiết kế UI thương mại để minh bạch gói phí và quyền sử dụng

Epic: SCRUM-8. Sprint: FA-02.

Mục tiêu: Thiết kế UI thương mại để minh bạch gói phí và quyền sử dụng.

Cần làm: Dựng gói, checkout, giấy phép, quota, chứng từ và đối soát; dùng dữ liệu mẫu, đánh dấu chính sách chưa chốt.

Phạm vi: SCRUM-30 (CUS-07), SCRUM-31 (CUS-08), SCRUM-32 (CUS-09), SCRUM-33 (CUS-10), SCRUM-34 (CUS-11), SCRUM-43 (OPS-02), SCRUM-44 (OPS-03), SCRUM-48 (OPS-07).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-02.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-65](https://phucnguyen31work.atlassian.net/browse/SCRUM-65) · UX-04.

### [SCRUM-66](https://phucnguyen31work.atlassian.net/browse/SCRUM-66) · UX-05 · Thiết kế UI nội dung để duyệt màu và khung trước phát hành

Epic: SCRUM-9. Sprint: FA-02.

Mục tiêu: Thiết kế UI nội dung để duyệt màu và khung trước phát hành.

Cần làm: Dựng thư viện, chỉnh công thức AI, khung và publish; mô phỏng preview, version, lỗi và quota.

Phạm vi: SCRUM-35 (CUS-12), SCRUM-36 (CUS-13), SCRUM-37 (CUS-14), SCRUM-38 (CUS-15).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-02.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-66](https://phucnguyen31work.atlassian.net/browse/SCRUM-66) · UX-05.

### [SCRUM-67](https://phucnguyen31work.atlassian.net/browse/SCRUM-67) · UX-06 · Thiết kế UI cập nhật để kiểm soát rollout và sự cố

Epic: SCRUM-10. Sprint: FA-02.

Mục tiêu: Thiết kế UI cập nhật để kiểm soát rollout và sự cố.

Cần làm: Dựng quản lý release và triển khai theo nhóm; thể hiện downloaded, installed, healthy, pause và rollback.

Phạm vi: SCRUM-45 (OPS-04), SCRUM-46 (OPS-05).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-02.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-67](https://phucnguyen31work.atlassian.net/browse/SCRUM-67) · UX-06.

### [SCRUM-68](https://phucnguyen31work.atlassian.net/browse/SCRUM-68) · UX-07 · Thiết kế UI nhận ảnh để giải thích rõ trạng thái QR

Epic: SCRUM-11. Sprint: FA-01.

Mục tiêu: Thiết kế UI nhận ảnh để giải thích rõ trạng thái QR.

Cần làm: Dựng QR tại booth và gallery di động; phân biệt đang upload, sẵn sàng, hết hạn, bị thu hồi và mất mạng.

Phạm vi: SCRUM-22 (BTH-11), SCRUM-50 (DL-01), SCRUM-51 (DL-02).

Đầu ra / nghiệm thu:
- Tất cả màn hình trong phạm vi có UI chạy bằng dữ liệu giả và luồng điều hướng kiểm chứng được.
- Có trạng thái loading/empty/error/offline, ma trận quyền và trường dữ liệu/hợp đồng tích hợp; chính sách mở được đánh dấu.
- Có biên bản duyệt và danh sách bàn giao; không gọi giao diện dữ liệu giả là chức năng hoàn chỉnh.

Giai đoạn: UI/UX. Sprint dự kiến: FA-01.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-68](https://phucnguyen31work.atlassian.net/browse/SCRUM-68) · UX-07.

### [SCRUM-69](https://phucnguyen31work.atlassian.net/browse/SCRUM-69) · QA-01 · Kiểm thử booth offline để chứng minh an toàn tiền và bản in

Epic: SCRUM-5. Sprint: FA-10.

Mục tiêu: Kiểm thử booth offline để chứng minh an toàn tiền và bản in.

Cần làm: Chạy trọn phiên trên Windows và cấu hình đại diện; thử restart/mất mạng/lỗi camera/printer, print unknown và receipt replay; hardware test chỉ sau khi được cho phép.

Phạm vi: SCRUM-13 (BTH-02), SCRUM-14 (BTH-03), SCRUM-15 (BTH-04), SCRUM-16 (BTH-05), SCRUM-17 (BTH-06), SCRUM-18 (BTH-07), SCRUM-19 (BTH-08), SCRUM-20 (BTH-09), SCRUM-21 (BTH-10), SCRUM-23 (BTH-12), SCRUM-60 (SVC-09).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-69](https://phucnguyen31work.atlassian.net/browse/SCRUM-69) · QA-01.

### [SCRUM-70](https://phucnguyen31work.atlassian.net/browse/SCRUM-70) · QA-02 · Kiểm thử phân quyền và kích hoạt để ngăn truy cập sai tổ chức

Epic: SCRUM-6. Sprint: FA-10.

Mục tiêu: Kiểm thử phân quyền và kích hoạt để ngăn truy cập sai tổ chức.

Cần làm: Thử quyền ở API và UI, truy cập chéo tenant, mã hết hạn, kích hoạt lặp, chuyển seat và audit; xác minh không lộ token hoặc ảnh.

Phạm vi: SCRUM-12 (BTH-01), SCRUM-24 (CUS-01), SCRUM-25 (CUS-02), SCRUM-29 (CUS-06), SCRUM-40 (CUS-17), SCRUM-49 (OPS-08), SCRUM-52 (SVC-01), SCRUM-61 (SVC-10).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-70](https://phucnguyen31work.atlassian.net/browse/SCRUM-70) · QA-02.

### [SCRUM-71](https://phucnguyen31work.atlassian.net/browse/SCRUM-71) · QA-03 · Kiểm thử fleet để phát hiện trạng thái cũ và lệnh không an toàn

Epic: SCRUM-7. Sprint: FA-11.

Mục tiêu: Kiểm thử fleet để phát hiện trạng thái cũ và lệnh không an toàn.

Cần làm: Mô phỏng nhiều booth, heartbeat đến muộn, mất mạng, lệnh lặp/hết hạn và kết quả unknown; kiểm tra tải theo quy mô đã chốt.

Phạm vi: SCRUM-26 (CUS-03), SCRUM-27 (CUS-04), SCRUM-28 (CUS-05), SCRUM-39 (CUS-16), SCRUM-41 (CUS-18), SCRUM-42 (OPS-01), SCRUM-47 (OPS-06), SCRUM-55 (SVC-04).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-11.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-71](https://phucnguyen31work.atlassian.net/browse/SCRUM-71) · QA-03.

### [SCRUM-72](https://phucnguyen31work.atlassian.net/browse/SCRUM-72) · QA-04 · Kiểm thử billing và giấy phép để tránh ghi phí hoặc cấp quyền sai

Epic: SCRUM-8. Sprint: FA-10.

Mục tiêu: Kiểm thử billing và giấy phép để tránh ghi phí hoặc cấp quyền sai.

Cần làm: Thử webhook lặp/sai thứ tự, hủy/hoàn tiền theo policy, offline grace, thuê/vĩnh viễn, quota và đối soát hai dòng tiền riêng.

Phạm vi: SCRUM-30 (CUS-07), SCRUM-31 (CUS-08), SCRUM-32 (CUS-09), SCRUM-33 (CUS-10), SCRUM-34 (CUS-11), SCRUM-43 (OPS-02), SCRUM-44 (OPS-03), SCRUM-48 (OPS-07), SCRUM-53 (SVC-02), SCRUM-54 (SVC-03).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-72](https://phucnguyen31work.atlassian.net/browse/SCRUM-72) · QA-04.

### [SCRUM-73](https://phucnguyen31work.atlassian.net/browse/SCRUM-73) · QA-05 · Kiểm thử nội dung và AI để bảo toàn phiên bản và usage

Epic: SCRUM-9. Sprint: FA-10.

Mục tiêu: Kiểm thử nội dung và AI để bảo toàn phiên bản và usage.

Cần làm: Thử job thất bại/lặp, quota, tính phí, preview/render, khung không hợp lệ, publish gián đoạn và nội dung cũ tại booth.

Phạm vi: SCRUM-35 (CUS-12), SCRUM-36 (CUS-13), SCRUM-37 (CUS-14), SCRUM-38 (CUS-15), SCRUM-56 (SVC-05), SCRUM-59 (SVC-08).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-73](https://phucnguyen31work.atlassian.net/browse/SCRUM-73) · QA-05.

### [SCRUM-74](https://phucnguyen31work.atlassian.net/browse/SCRUM-74) · QA-06 · Kiểm thử rollout và rollback để bảo vệ booth đang phục vụ

Epic: SCRUM-10. Sprint: FA-11.

Mục tiêu: Kiểm thử rollout và rollback để bảo vệ booth đang phục vụ.

Cần làm: Thử gói lỗi/chữ ký sai, tải gián đoạn, thiếu dung lượng, phiên đang trả tiền, canary lỗi và phục hồi schema tương thích; giữ bằng chứng Windows.

Phạm vi: SCRUM-45 (OPS-04), SCRUM-46 (OPS-05), SCRUM-57 (SVC-06).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-11.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-74](https://phucnguyen31work.atlassian.net/browse/SCRUM-74) · QA-06.

### [SCRUM-75](https://phucnguyen31work.atlassian.net/browse/SCRUM-75) · QA-07 · Kiểm thử giao ảnh để bảo vệ file và phục hồi upload

Epic: SCRUM-11. Sprint: FA-10.

Mục tiêu: Kiểm thử giao ảnh để bảo vệ file và phục hồi upload.

Cần làm: Thử upload lặp/gián đoạn, QR trước khi có file, token hết hạn/thu hồi, truy cập sai bộ ảnh và xóa theo retention đã duyệt.

Phạm vi: SCRUM-22 (BTH-11), SCRUM-50 (DL-01), SCRUM-51 (DL-02), SCRUM-58 (SVC-07).

Đầu ra / nghiệm thu:
- Có ma trận test liên kết tiêu chí BA, dữ liệu kiểm thử và kết quả pass/fail tái hiện được.
- Các lỗi chặn phát hành được xử lý và chạy lại; phần thiếu thiết bị/chính sách ghi Blocked, không ghi Pass.
- Lưu báo cáo và bằng chứng trong pipeline/tài liệu nghiệm thu; đạt tiêu chí đã duyệt trước khi đóng Task.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-75](https://phucnguyen31work.atlassian.net/browse/SCRUM-75) · QA-07.

### [SCRUM-76](https://phucnguyen31work.atlassian.net/browse/SCRUM-76) · CICD-01 · Hoàn thiện CI để kiểm tra tự động và truy vết từng bản build

Epic: SCRUM-10. Sprint: FA-10.

Mục tiêu: Hoàn thiện CI để kiểm tra tự động và truy vết từng bản build.

Cần làm: Mở rộng pipeline hiện có cho booth, web và backend; restore/build/lint, unit/integration, harness, kiểm tra dependency/secret, lưu báo cáo và artifact gắn commit. Chạy lỗi có chủ đích để xác minh gate chặn; ghi rõ test nào cần Windows/hardware.

Nghiệm thu:
- Có cấu hình được review và lần chạy thành công lưu bằng chứng.
- Kịch bản lỗi chặn phát hành hoặc phục hồi theo runbook được kiểm chứng.
- Bàn giao tài liệu vận hành, quyền phê duyệt và các giới hạn còn lại.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-10.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-76](https://phucnguyen31work.atlassian.net/browse/SCRUM-76) · CICD-01.

### [SCRUM-77](https://phucnguyen31work.atlassian.net/browse/SCRUM-77) · CICD-02 · Thiết lập CD và diễn tập phát hành để triển khai có thể phục hồi

Epic: SCRUM-10. Sprint: FA-11.

Mục tiêu: Thiết lập CD và diễn tập phát hành để triển khai có thể phục hồi.

Cần làm: Thiết lập staging, quản lý secret, migration/backup, ký và đóng gói Windows, promotion có phê duyệt, health check, rollback và runbook; diễn tập trên môi trường thử nghiệm trước pilot. Không tự triển khai production trong task lập kế hoạch.

Nghiệm thu:
- Có cấu hình được review và lần chạy thành công lưu bằng chứng.
- Kịch bản lỗi chặn phát hành hoặc phục hồi theo runbook được kiểm chứng.
- Bàn giao tài liệu vận hành, quyền phê duyệt và các giới hạn còn lại.

Giai đoạn: Test, CI/CD. Sprint dự kiến: FA-11.
Nguồn: docs/ba/jira/sprint-plan.md.
Mã kế hoạch: [SCRUM-77](https://phucnguyen31work.atlassian.net/browse/SCRUM-77) · CICD-02.

