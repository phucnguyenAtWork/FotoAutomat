# Backlog đã duyệt cho Jira SCRUM

**7 Epic, 40 Story theo màn hình, 10 Task nền tảng.** Đã tạo trên Jira: xem [bảng liên kết SCRUM](published.md). Mã BA là mã tham chiếu, không phải Jira key. Không gán sprint, người phụ trách hoặc ước lượng khi chưa có dữ liệu.

Tiêu đề mô tả hành động và mục đích. Mô tả mỗi ticket gồm mục tiêu, phạm vi, ba tiêu chí nghiệm thu, phụ thuộc và đường dẫn BA. Đây là backlog tính năng để thiết kế/triển khai, không chỉ là việc viết tài liệu.

## EP-01 · Vận hành phiên chụp tại booth khi mất mạng

Đảm bảo khách thanh toán, chụp, xử lý và in tại booth; bảo toàn phiên khi thiết bị hoặc kết nối gặp lỗi.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| BTH-02|Story|[Kiểm tra thiết bị để xác nhận booth sẵn sàng phục vụ](../screens/bth-02.md) |
| BTH-03|Story|[Hiển thị màn hình chào để bắt đầu phiên chụp](../screens/bth-03.md) |
| BTH-04|Story|[Cho chọn gói và màu để xác nhận sản phẩm trước thanh toán](../screens/bth-04.md) |
| BTH-05|Story|[Ghi nhận tiền tại booth để mở phiên chụp hợp lệ](../screens/bth-05.md) |
| BTH-06|Story|[Điều khiển live view và chụp để lưu đủ ảnh của phiên](../screens/bth-06.md) |
| BTH-07|Story|[Cho chọn ảnh để xác định nội dung bản in](../screens/bth-07.md) |
| BTH-08|Story|[Cho chỉnh làm đẹp để khách duyệt ảnh sau chụp](../screens/bth-08.md) |
| BTH-09|Story|[Cho chọn khung và xác nhận để gửi đúng bản in](../screens/bth-09.md) |
| BTH-10|Story|[Theo dõi kết quả in để xử lý lỗi và tránh in trùng](../screens/bth-10.md) |
| BTH-12|Story|[Đối soát phiên gián đoạn để khôi phục phục vụ tại booth](../screens/bth-12.md) |
| SVC-09|Task|[Điều phối thiết bị và phục hồi phiên để tránh mất tiền hoặc in trùng](../services/svc-09.md) |

## EP-02 · Quản lý tài khoản và kích hoạt thiết bị theo tổ chức

Phân tách khách hàng thương mại, đăng ký booth đúng chủ sở hữu và kiểm soát quyền truy cập.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| BTH-01|Story|[Kích hoạt booth để gắn thiết bị với giấy phép](../screens/bth-01.md) |
| CUS-01|Story|[Xác thực người dùng để truy cập đúng tổ chức](../screens/cus-01.md) |
| CUS-02|Story|[Thiết lập tổ chức để quản lý hoạt động thương mại](../screens/cus-02.md) |
| CUS-06|Story|[Cung cấp bộ cài và mã kích hoạt để triển khai booth mới](../screens/cus-06.md) |
| CUS-17|Story|[Phân quyền thành viên để giới hạn truy cập theo vai trò](../screens/cus-17.md) |
| OPS-08|Story|[Tra cứu audit để truy vết thay đổi trên hệ thống](../screens/ops-08.md) |
| SVC-01|Task|[Đăng ký thiết bị để ràng buộc booth với tổ chức và seat](../services/svc-01.md) |
| SVC-10|Task|[Cô lập dữ liệu và ghi audit để bảo vệ từng khách hàng](../services/svc-10.md) |

## EP-03 · Giám sát booth và xử lý sự cố từ xa

Theo dõi trạng thái, phiên bản thực tế và lịch sử thao tác của từng booth với dữ liệu có thời điểm xác minh.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| CUS-03|Story|[Tổng hợp tình trạng tài khoản để nhận biết việc cần xử lý](../screens/cus-03.md) |
| CUS-04|Story|[Liệt kê và lọc booth để quản lý theo tổ chức](../screens/cus-04.md) |
| CUS-05|Story|[Hiển thị chi tiết booth để theo dõi và yêu cầu tác vụ từ xa](../screens/cus-05.md) |
| CUS-16|Story|[Tra cứu phiên chụp để theo dõi doanh thu và giao file](../screens/cus-16.md) |
| CUS-18|Story|[Theo dõi yêu cầu hỗ trợ để xử lý sự cố có căn cứ](../screens/cus-18.md) |
| OPS-01|Story|[Giám sát fleet để phát hiện booth mất liên lạc hoặc lỗi](../screens/ops-01.md) |
| OPS-06|Story|[Đối soát lệnh từ xa để xử lý kết quả chưa xác định](../screens/ops-06.md) |
| SVC-04|Task|[Theo dõi heartbeat và lệnh để biết trạng thái thực của booth](../services/svc-04.md) |

## EP-04 · Kinh doanh bản quyền và dịch vụ trả phí

Hỗ trợ thuê, mua vĩnh viễn và dịch vụ bổ sung; đối soát thanh toán và quyền sử dụng rõ ràng.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| CUS-07|Story|[So sánh gói sản phẩm để khách chọn đúng quyền sử dụng](../screens/cus-07.md) |
| CUS-08|Story|[Thanh toán dịch vụ để cấp quyền theo đơn hàng](../screens/cus-08.md) |
| CUS-09|Story|[Quản lý giấy phép để theo dõi seat, thời hạn và gia hạn](../screens/cus-09.md) |
| CUS-10|Story|[Hiển thị usage và quota để kiểm soát chi phí dịch vụ](../screens/cus-10.md) |
| CUS-11|Story|[Tra cứu giao dịch để đối soát phí và tải chứng từ](../screens/cus-11.md) |
| OPS-02|Story|[Tổng hợp hồ sơ khách hàng để hỗ trợ đúng tổ chức](../screens/ops-02.md) |
| OPS-03|Story|[Điều chỉnh quyền sử dụng để thực thi chính sách thương mại](../screens/ops-03.md) |
| OPS-07|Story|[Đối soát billing và usage để tránh ghi phí sai](../screens/ops-07.md) |
| SVC-02|Task|[Quản lý quyền sử dụng để hỗ trợ thuê, vĩnh viễn và offline](../services/svc-02.md) |
| SVC-03|Task|[Đối soát thanh toán để cấp quyền và ghi phí chính xác](../services/svc-03.md) |

## EP-05 · Tạo và phân phối công thức màu, khung ảnh

Cho phép tạo màu bằng AI, tinh chỉnh và phát hành nội dung tương thích tới đúng booth.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| CUS-12|Story|[Quản lý thư viện màu để tái sử dụng phiên bản đã duyệt](../screens/cus-12.md) |
| CUS-13|Story|[Tạo và tinh chỉnh màu bằng AI để xuất công thức cho booth](../screens/cus-13.md) |
| CUS-14|Story|[Thiết kế khung ảnh để tạo bố cục in tương thích](../screens/cus-14.md) |
| CUS-15|Story|[Phát hành nội dung để đồng bộ đúng phiên bản tới booth](../screens/cus-15.md) |
| SVC-05|Task|[Đồng bộ nội dung có phiên bản để booth dùng dữ liệu nhất quán](../services/svc-05.md) |
| SVC-08|Task|[Xử lý AI và đo usage để cung cấp dịch vụ có phí minh bạch](../services/svc-08.md) |

## EP-06 · Phân phối và cập nhật phần mềm booth có kiểm soát

Quản lý bộ cài, phiên bản, rollout và khôi phục mà không gián đoạn phiên đã trả tiền.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| OPS-04|Story|[Quản lý release để phân phối bộ cài đã được kiểm chứng](../screens/ops-04.md) |
| OPS-05|Story|[Triển khai cập nhật theo đợt để hạn chế ảnh hưởng toàn fleet](../screens/ops-05.md) |
| SVC-06|Task|[Cập nhật booth an toàn để triển khai phiên bản trên toàn fleet](../services/svc-06.md) |

## EP-07 · Giao ảnh qua QR và bảo vệ quyền truy cập

Cung cấp file đúng phiên, hiển thị tình trạng giao file và giới hạn truy cập theo chính sách.

| Mã BA | Loại | Công việc |
| --- | --- | --- |
| BTH-11|Story|[Hiển thị QR để giao file và kết thúc phiên an toàn](../screens/bth-11.md) |
| DL-01|Story|[Xác minh QR để thông báo tình trạng bộ ảnh](../screens/dl-01.md) |
| DL-02|Story|[Cung cấp gallery để khách tải đúng file được cấp quyền](../screens/dl-02.md) |
| SVC-07|Task|[Xếp hàng giao file để tiếp tục phục vụ khi mất mạng](../services/svc-07.md) |

