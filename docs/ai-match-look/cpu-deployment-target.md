# Cấu hình triển khai CPU — quyết định 06/10/2026

Người dùng chốt ASUS NUC 13 Pro Slim, Intel Core i7-1360P, RAM 16 GB làm cấu hình mục tiêu FotoAutomat. Không yêu cầu GPU rời. Đây là quyết định phát triển, chưa phải chứng nhận hiệu năng trên thiết bị thật hoặc yêu cầu mua máy.

Cấu hình đề xuất đi kèm: RAM 2 × 8 GB, SSD NVMe 512 GB, Windows 11. Dung lượng SSD và bản quyền Windows cần kiểm tra khi đặt máy; không coi tên “i7” của model khác là cùng hiệu năng.

## Cách triển khai

1. Giữ baseline PyTorch CPU FP32 và checkpoint/hash hiện có. Đo nhiều lượt, thời gian nạp model và peak working set trước khi đổi runtime.
2. Thử xuất ONNX và đối chiếu tensor/LUT/ảnh với baseline. Chỉ chọn ONNX Runtime CPU khi export chạy được, màu được bảo toàn và có số đo tốt hơn; chưa có ONNX đã kiểm chứng.
3. Tách tạo look trong quản trị khỏi áp look trong luồng khách. Cache LUT và metadata theo phiên bản model, reference và các lựa chọn xử lý. Không chạy model đầy đủ cho từng frame live view.
4. Giữ đủ hai bước canonicalization và restyling của CanonCGT khi tối ưu; không mặc định LUT phụ thuộc source có thể tái dùng cho mọi ảnh. LUT xuất cố định cần kiểm tra nhiều ảnh. Chuẩn hóa Exposure/WB từng ảnh là bước riêng; mask vùng da không thể gói đầy đủ vào LUT toàn ảnh.
5. Tích hợp tác vụ nền có hủy và giới hạn đồng thời để không chặn WPF. Train/fine-tune thực hiện trên máy khác nếu cần GPU.

## Đo và nghiệm thu

`Run-CanonCgtCpu.py` hỗ trợ `--threads 4 --warmup 1 --repeats 5`. Báo cáo giữ từng mẫu, median/p95 forward, thời gian nạp checkpoint + dựng model, peak working set của toàn tiến trình Windows. Forward không bao gồm đọc/ghi ảnh hoặc khởi động Python; peak RAM bao gồm cả tạo ảnh so sánh. Năm cảnh chỉ là smoke benchmark, chưa đại diện chân dung hoặc độ ổn định dài hạn.

Trên NUC thật cần đo tiếp: cold start toàn tiến trình, peak RAM của toàn ứng dụng và worker, ảnh kích thước camera thực, live view đồng thời xử lý/in, hủy tác vụ, chạy liên tục và nhiệt độ. Chưa đặt con số độ trễ thành cam kết. Chất lượng màu/da và quyền dataset vẫn là gate độc lập với tốc độ.

## Trạng thái

Đã chốt cấu hình mục tiêu và bổ sung công cụ đo lặp CPU. Chưa tích hợp CanonCGT/ONNX vào WPF, chưa train thêm, chưa kiểm thử trên NUC thật.

Lần chạy local 06/10/2026: SSL_updated_251111, PyTorch CPU, 4 threads, 5 cảnh PST50 1920×1080, warmup 1 lần/cảnh rồi đo 5 lần/cảnh (25 mẫu). Median forward 0,353 giây, p95 0,367 giây; peak working set tiến trình khoảng 592 MiB. Báo cáo tại `.local/matchlook-data/cpu-target-benchmark-001/report.json`. Đây là máy phát triển hiện tại, không phải kết quả NUC i7-1360P. Truy vấn tên CPU/RAM qua CIM bị từ chối quyền; không suy đoán model máy. Không đo toàn WPF, camera hoặc in trong lượt này.
