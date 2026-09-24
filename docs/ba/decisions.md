# Sổ quyết định nghiệp vụ

Tất cả mục dưới đây đang **Mở**. Người phụ trách là vai trò đề xuất, chưa phải cá nhân đã được giao. Cần duyệt những mục ảnh hưởng Story trước khi triển khai thương mại. Không cần trả lời toàn bộ chỉ để đọc wireframe.

<a id="d-01"></a>
## D-01 · Sản phẩm, thuê và mua vĩnh viễn

* Trạng thái: Mở. Người quyết định đề xuất: Chủ sản phẩm / thương mại.
* Cần làm rõ: Chốt quyền desktop, số seat, quyền cập nhật, hỗ trợ và dịch vụ cloud của từng sản phẩm. Xác định điều gì còn dùng được sau khi thuê bao dịch vụ hết hạn.
* Đề xuất để review: Đề xuất tách quyền dùng desktop, quyền cập nhật và cloud; chưa đặt giá, thời hạn hoặc mức quota.

<a id="d-02"></a>
## D-02 · Kích hoạt, seat và chuyển máy

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / vận hành / kỹ thuật.
* Cần làm rõ: Chốt mã kích hoạt, giữ chỗ seat, định danh thiết bị, thay máy hỏng, chuyển tổ chức và thu hồi thiết bị cũ.
* Đề xuất để review: Kích hoạt lần đầu qua server; thao tác lặp không chiếm thêm seat; mọi chuyển giao có audit.

<a id="d-03"></a>
## D-03 · Quyền offline và hết hạn

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / thương mại / kỹ thuật.
* Cần làm rõ: Chốt thời gian offline được phép, nguồn thời gian tin cậy, xử lý đổi đồng hồ, quá hạn thuê và thu hồi quyền khi máy không kết nối.
* Đề xuất để review: Giữ phiên đã nhận tiền để hoàn tất/đối soát; ngăn phiên mới theo policy. Không tự chọn số ngày grace period.

<a id="d-04"></a>
## D-04 · Thanh toán khách chụp

* Trạng thái: Mở. Người quyết định đề xuất: Chủ sản phẩm / đơn vị vận hành booth.
* Cần làm rõ: Chốt vị trí thanh toán trong luồng, tiền tệ, mệnh giá, tiền thừa, nhận thiếu, timeout, hủy, hoàn tiền và bằng chứng đối soát.
* Đề xuất để review: Luồng dự thảo chọn gói/màu rồi trả tiền; không khẳng định bộ nhận tiền có khả năng hoàn tiền.

<a id="d-05"></a>
## D-05 · Danh tính và phân quyền

* Trạng thái: Mở. Người quyết định đề xuất: Chủ sản phẩm / quản trị an toàn.
* Cần làm rõ: Chốt phương thức login, xác thực mạnh cho staff, role/tenant/location scope, owner cuối cùng, quyền support và xác thực operator offline.
* Đề xuất để review: Kiểm tra quyền ở server; staff và tenant roles riêng; không coi tài khoản khách hàng là tài khoản Ops.

<a id="d-06"></a>
## D-06 · QR và giao file khi mất mạng

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / trải nghiệm khách.
* Cần làm rõ: Chốt file được giao, claim link cấp trước hay thông báo nhận sau, kênh hỗ trợ khi offline, expiry và cách khách lưu ảnh.
* Đề xuất để review: Không báo sẵn sàng trước upload/publish; link chỉ truy cập đúng phiên.

<a id="d-07"></a>
## D-07 · Phần cứng và protocol

* Trạng thái: Mở. Người quyết định đề xuất: Kỹ thuật phần cứng / vận hành.
* Cần làm rõ: Chốt camera/SDK và quyền phân phối, model money acceptor, protocol, loại sự kiện và khả năng hồi phục sau mất điện.
* Đề xuất để review: Phải kiểm thử trên máy thật; BA không coi X-H2/R50/R100 là đã được xác nhận tương thích.

<a id="d-08"></a>
## D-08 · Gói chụp và bản in

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / vận hành booth.
* Cần làm rõ: Chốt số lần chụp/chọn/retake, số bản, khổ giấy, crop, mức làm đẹp, trạng thái hoàn thành in và quyền in lại.
* Đề xuất để review: Lệnh in unknown cần operator đối soát; không tự in lại hoặc coi spooler accepted là hoàn tất vật lý.

<a id="d-09"></a>
## D-09 · Recipe, AI và quota

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / chuyên môn hình ảnh / kỹ thuật.
* Cần làm rõ: Chốt loại recipe local, renderer và color management, AI provider/model, dữ liệu tham khảo, cách đo job thành công/lỗi/hủy và quyền publish.
* Đề xuất để review: AI hỗ trợ authoring ngoài booth; nội dung phát hành có version và phải chạy được trên booth mục tiêu.

<a id="d-10"></a>
## D-10 · Ảnh khách, quyền riêng tư và lưu giữ

* Trạng thái: Mở. Người quyết định đề xuất: Chủ sản phẩm / người phụ trách dữ liệu.
* Cần làm rõ: Chốt ai được xem ảnh, thời gian giữ local/cloud/audit, xóa theo yêu cầu, file chưa giao, backup và thông báo cho người chụp.
* Đề xuất để review: Không để khách sau thấy phiên trước; không đặt thời hạn retention hoặc quyền xem ảnh mặc định cho owner tenant.

<a id="d-11"></a>
## D-11 · Môi trường và tiêu chí hiệu năng

* Trạng thái: Mở. Người quyết định đề xuất: Kỹ thuật / vận hành.
* Cần làm rõ: Chốt Windows được hỗ trợ, CPU/RAM/storage, màn hình, độ phân giải ảnh và ngân sách thời gian cho preview/render/chụp/in.
* Đề xuất để review: Đo trên máy cấu hình thấp đại diện; không có GPU rời là giả định thiết kế, chưa có số benchmark.

<a id="d-12"></a>
## D-12 · Billing dịch vụ và chứng từ

* Trạng thái: Mở. Người quyết định đề xuất: Thương mại / tài chính.
* Cần làm rõ: Chốt provider thanh toán, thị trường/tiền tệ, chu kỳ, gia hạn, prorate, refund, overage, chứng từ và phí dịch vụ hỗ trợ.
* Đề xuất để review: Chỉ xác minh payment ở server; không tự hứa chính sách thuế, giá hay SLA.

<a id="d-13"></a>
## D-13 · Cập nhật, điều khiển từ xa và rollback

* Trạng thái: Mở. Người quyết định đề xuất: Release / vận hành / chủ sản phẩm.
* Cần làm rõ: Chốt danh mục lệnh, thời hạn command, quyền tự cập nhật/opt-in, maintenance window, canary, health gate, điều kiện dừng và rollback dữ liệu.
* Đề xuất để review: Không restart giữa phiên trả tiền; phân biệt downloaded/installed/healthy và chỉ rollback khi tương thích.

<a id="d-14"></a>
## D-14 · Báo cáo và định nghĩa sức khỏe

* Trạng thái: Mở. Người quyết định đề xuất: Sản phẩm / vận hành / tài chính.
* Cần làm rõ: Chốt ngưỡng stale/offline, thời gian báo cáo, doanh thu theo tiền nhận hay fulfillment, refund, dữ liệu đến muộn và chỉ số đầy đủ.
* Đề xuất để review: Hiện last seen và phạm vi dữ liệu; không dùng giá trị 0 để thay trạng thái truy vấn lỗi.
