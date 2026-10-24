# Checklist chụp kiểm chuẩn booth — chưa thực hiện

Mục tiêu: đo đầu vào thật dưới cấu hình khóa camera + flash thủ công + đèn liên tục; so live view với ảnh cuối trước khi chọn profile/normalization. Tài liệu không cấp quyền điều khiển camera hay flash tự động.

## Chuẩn bị

- Mỗi camera tạo mã cấu hình riêng: model/serial nội bộ, lens, firmware, ISO, shutter, aperture, custom WB/Kelvin+tint nếu có, JPEG picture style/film simulation và các thông số con, color space, RAW format, flash power/modifier, vị trí đèn và vị trí người.
- Khóa exposure/WB/flash; kiểm tra camera không tự đổi ISO, dynamic-range mode hoặc tone enhancement. Ghi chính xác các chế độ thực sự hỗ trợ, chưa giả định SDK điều khiển được.
- Giữ đèn liên tục sáng trong cả hai luồng. Đánh dấu tâm và các vị trí ngoài của nhóm4người. Dùng thẻ xám/bảng màu chuẩn nếu có; không dùng giấy trắng thay chuẩn đo màu.

## Thu dữ liệu

1. Chụp bảng màu tại vị trí mặt ở tâm và các vị trí ngoài, tránh phản xạ bóng trên bảng. Mỗi vị trí3lần, lưu RAW+JPEG gốc không chỉnh.
2. Lưu frame live view gần cùng thời điểm, cùng crop nếu khả thi; ghi độ phân giải, transport/SDK và timestamp. Live view chỉ thấy đèn liên tục, capture thấy đèn liên tục+flash.
3. Chụp1người và nhóm4người có đồng ý sử dụng ảnh; lặp3lần/cấu hình. Kiểm tra mắt, da, trang phục trắng/đen, nền và người ở rìa.
4. Nếu vận hành cho phép, tạo cấu hình thử sáng/tối lệch nhỏ và WB khác quanh baseline; lưu thành cấu hình riêng, không trộn nhãn. Không chỉ định EV/shutter gây nguy cơ sai flash sync; thông số phải chọn theo thiết bị thực tế.
5. Lặp một lượt khác sau khi setup lại/phiên khác để làm validation. Không dùng các bản cùng ảnh hoặc cùng burst vào cả train và test.

## Giao dữ liệu và kiểm tra

Giữ cấu trúc config-id/session-id/{raw,jpeg,liveview}, cùng manifest config. Nếu chưa có live-view export hay chart, ghi missing, không giả lập bằng JPEG cuối.

Đo riêng: độ lặp sáng/màu giữa lần chụp; neutral/chart color error khi có reference phù hợp; skin/highlight/background; khác biệt preview/capture; thời gian RAW decode và bộ nhớ. Giá trị RGB/JPEG không đủ chứng minh sensor clipping: dùng RAW inspection/processing history khi có. Không cam kết skin phải có một màu chuẩn duy nhất.

Giữ reference look mong muốn riêng; chuẩn hóa orientation/ICC là kỹ thuật decode, không tự neutralize phong cách reference. DNG camera profile không trực tiếp tương đương JPEG correction matrix.

Đầu ra mong đợi: manifest cấu hình, paired capture/liveview, báo cáo lỗi + thiếu dữ liệu, đề xuất transform tách preview/capture và ablation dùng cùng split. Chưa tự train/phát hành preset.
