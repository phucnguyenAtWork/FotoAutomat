# FotoAutomat · Bộ BA màn hình và backlog thương mại

Phiên bản: **0.1 · Đã duyệt xuất bản backlog; chính sách mở cần refinement**. Ngày: 2026-09-24.

Mục tiêu: xác định mỗi người dùng cần làm gì, đi qua màn hình nào, thấy dữ liệu gì và hệ thống phải phản hồi thế nào. Đây là yêu cầu thiết kế/triển khai cho sản phẩm thương mại, không phải xác nhận các tính năng đã tồn tại.

## Phạm vi đã xác nhận

* Phần mềm booth Windows được phân phối cho khách mua/thuê và vận hành trên máy cấu hình thấp.
* Booth tiếp tục nhận tiền, chụp, xử lý và in khi mất Internet; upload/sync có thể chờ.
* Website là cổng khách hàng quản lý booth và sử dụng dịch vụ có phí.
* FotoAutomat cần quản lý việc lắp đặt, phiên bản và đồng bộ nhiều booth.
* Có định hướng thuê và mua vĩnh viễn; điều khoản cụ thể chưa chốt.
* Khách chụp chọn màu, chụp, làm đẹp, chọn khung, in và nhận file bằng QR.
* Người quản trị tạo công thức màu từ tham khảo bằng AI và tinh chỉnh.
* Jira đích: project SCRUM tại phucnguyen31work.atlassian.net.

## Điểm bắt đầu

| Tài liệu | Mục đích |
| --- | --- |
| [Danh mục và luồng màn hình](screen-map.md) | Tra cứu 40 trang BA và đường đi UI |
| [Vai trò và quyền](roles.md) | Phân biệt khách chụp, chủ booth và staff FotoAutomat |
| [Quy tắc UX dùng chung](ux-principles.md) | Loading, offline, lỗi, pending, dữ liệu cũ và thao tác nhạy cảm |
| [Quyết định cần chốt](decisions.md) | 14 nhóm câu hỏi kinh doanh/thiết bị còn mở |
| [Backlog Jira đã xuất bản](jira/published.md) | 7 Epic, 40 Story màn hình và 10 Task nền tảng |
| [Kế hoạch 3 giai đoạn và sprint](jira/sprint-plan.md) | Mục tiêu UI/UX, Backend, Test CI/CD; 11 sprint và đầu ra |
| [Hướng dẫn Jira](jira/README.md) | Cách đưa nội dung vào project và theo dõi liên kết |

## Phân vùng sản phẩm

| Phần | Số màn hình | Người sử dụng |
| --- | --- | --- |
| BTH · Desktop booth | 12 | Khách chụp, operator cục bộ, kỹ thuật viên |
| CUS · Cổng khách hàng | 18 | Chủ booth, billing, quản lý nội dung, vận hành |
| OPS · Trang nội bộ FotoAutomat | 8 | Staff account, support, release, tài chính, audit |
| DL · Nhận file qua QR | 2 | Người chụp có link được cấp quyền |

OPS là một vùng quyền riêng. CUS không được thấy dữ liệu tất cả khách hàng. Hai vùng có thể cùng một sản phẩm web nhưng phải tách kiểm soát truy cập.

## Cách đọc một BA page

Mỗi page có mục tiêu, vai trò, điều hướng, wireframe nội dung, dữ liệu và nguồn, hành động, luồng chính, quy tắc nghiệp vụ, trạng thái ngoại lệ, ba tiêu chí nghiệm thu và quyết định còn mở. Wireframe bằng chữ mô tả cấu trúc chức năng; chưa chọn màu sắc, font, kích thước pixel hay phong cách hình ảnh.

Mã BTH/CUS/OPS/DL là mã BA bền vững. Mã SCRUM đã được lưu trong [bảng liên kết Jira](jira/published.md). Không dùng mã BA như issue key hoặc đường link đã tồn tại trên Jira.

## Ranh giới thương mại

Có hai dòng tiền: khách chụp trả cho đơn vị vận hành booth và đơn vị đó trả phí phần mềm/dịch vụ cho FotoAutomat. Hai dòng tiền có dữ liệu, báo cáo và quy trình hoàn tiền khác nhau.

Giấy phép desktop, quyền cập nhật và quyền dùng dịch vụ cloud là các quyền riêng. Mô hình chính xác phải được duyệt ở D-01. Việc chọn SQLite ở simulator không quyết định công nghệ máy chủ hoặc mô hình billing.

## Trạng thái và thứ tự làm việc

Backlog đã được duyệt và tạo trên Jira. Nội dung BA vẫn cần refinement trước triển khai. Trước khi kéo một Story vào triển khai: duyệt quyết định liên quan, review wireframe/trạng thái với người phụ trách sản phẩm, xác định nguồn dữ liệu/quyền và làm rõ test phụ thuộc thiết bị. Không coi một Story sẵn sàng thương mại chỉ vì đã có giao diện đẹp.

Đề xuất thứ tự: chốt chính sách → kích hoạt/quyền → một booth phục vụ trọn phiên → giám sát/sync → dịch vụ sáng tạo và giao file → rollout thương mại. Cập nhật có kiểm soát và bảo vệ dữ liệu là điều kiện trước khi mở rộng fleet, không phải phần bỏ qua trong sản phẩm bán thật.

Bộ BA không tự chốt giá, nhà cung cấp thanh toán, framework máy chủ, AI model, retention, SLA hoặc quyền sử dụng thương mại của SDK. Chúng được quản lý ở sổ quyết định.
