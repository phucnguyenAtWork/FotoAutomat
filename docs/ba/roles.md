# Vai trò và quyền đề xuất

Trạng thái: cần duyệt D-05. Đây là mô hình quyền chức năng; chưa chốt provider đăng nhập hay cơ chế xác thực offline.

| Vai trò | Phạm vi | Quyền dự kiến | Hạn chế chính |
| --- | --- | --- | --- |
| Khách chụp | Phiên hiện tại trên booth | Chọn sản phẩm, chụp, chỉnh ảnh, in, nhận QR | Không mở cài đặt hay dữ liệu phiên khác |
| Người nhận ảnh | Bộ ảnh được token cấp quyền | Xem/tải trong thời hạn | Không suy ra quyền từ biết session ID |
| Chủ tổ chức | Tenant của mình | Quản lý sản phẩm, thiết bị và thành viên | Không quản lý tenant khác |
| Vận hành | Booth/nhóm được cấp | Theo dõi, hỗ trợ và thao tác cho phép | Không mặc định có quyền billing hoặc xem ảnh khách |
| Thiết kế nội dung | Thư viện nội dung tenant | Tạo/chỉnh recipe và frame | Quyền publish có thể tách khỏi quyền chỉnh |
| Billing | Thương mại tenant | Mua/gia hạn, xem phí/chứng từ | Không mặc định điều khiển thiết bị |
| Người xem báo cáo | Dữ liệu được cấp | Xem metadata và số liệu | Không mặc định tải ảnh khách |
| Staff support | Tenant/incident được cấp | Xem chẩn đoán và xử lý hỗ trợ | Không tự có quyền refund, thu hồi license hay truy cập ảnh |
| Staff release | Release và rollout | Quản lý artifact và phân phối | Cần tuân thủ health/idle/compatibility gates |
| Staff tài chính | Billing hệ thống | Đối soát và điều chỉnh theo thẩm quyền | Không sửa lịch sử giao dịch gốc |
| Auditor | Audit theo phạm vi | Tra cứu/xuất theo quyền | Không tự thay đổi nghiệp vụ |

Quyền kiểm tra ở server và ở runtime booth đối với thao tác local. UI ẩn nút không đủ để bảo vệ dữ liệu. Thay tenant trong URL/request phải bị kiểm soát.

Chọn tenant rõ ở cổng khách hàng. Nhân sự nội bộ luôn thấy tenant đích khi thao tác nhạy cảm. Lời mời, thu hồi quyền, chuyển owner cuối cùng và truy cập support tạm thời cần tiêu chí riêng trong D-05.

Thông tin tổ chức, thanh toán dịch vụ và ảnh người chụp là ba loại dữ liệu khác nhau; xem một loại không mặc nhiên được xem tất cả.
