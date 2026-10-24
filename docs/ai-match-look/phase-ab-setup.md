# Phase A/B — thiết lập 06/10/2026

Cập nhật 07/10: setup bên dưới là kế hoạch ban đầu. Phase A đã có báo cáo audit; [Phase B đã tạo pilot](phase-b-results-2026-10-07.md) 96 triplets. Người dùng xác nhận ba buổi riêng và ảnh đã có preset; chưa đóng gate training.

Người dùng đồng ý hướng supervised fine-tuning sau baseline, bổ sung ảnh riêng và xác nhận đã được chấp thuận sử dụng. Chấp nhận xác nhận này cho batch riêng; không áp quyền đó sang Open Images. Ảnh, đường dẫn và manifest chi tiết chỉ nằm trong `.local/`, không đưa vào Git hay gửi ra mạng.

## A — kiểm chứng CanonCGT

Thứ tự thực hiện: (1) ghi commit/hash code/checkpoint và strict-load cả E2E/SSL; (2) test identity LUT với RGB ramp để bắt lỗi trục/kênh; (3) chạy official samples theo demo và so adapter; (4) cố định source, đổi reference neutral/warm/cool/B&W; (5) đổi source cùng look; (6) kiểm tra deterministic, finite, clipping và ảnh hai bước canonical/restyle; (7) xác định policy ICC/EXIF/range.

Đầu ra: báo cáo JSON, bảng ảnh source/reference/canonical/result, số đo thay đổi theo reference, sai lệch so official, thời gian/RAM. Source=reference là diagnostic, không yêu cầu output bằng input tuyệt đối. Gate A: giải thích được kết quả transfer yếu; không kết luận chất lượng đạt chỉ vì load weights thành công. Chưa chạy xong A trong lượt setup.

## B — dataset và benchmark

Inventory giữ hash/originals; manifest pilot ghi quyền user-confirmed, role/look/session/subject/split chưa gán. Tạm giữ cả batch trong một leakage group chưa phân giải. Không suy diễn danh tính hoặc phiên từ tên file; không chia ngẫu nhiên ảnh liên tiếp.

Giữ look đen trắng, low-key, ám màu có chủ đích. Gán 8 nhóm khởi đầu: natural/warm/cool/pastel/film/high-contrast/low-key/monochrome; cho phép nhiều nhãn và bổ sung nhóm. Chưa coi đây là nhãn thực tế của ảnh đã nhập.

Mẫu benchmark gồm source + reference khác nội dung + target cùng nội dung/căn chỉnh với source + look_id + phê duyệt của người duyệt. Không tạo target bằng model rồi coi đó là ground truth độc lập. Không tự suy ra JPEG là ảnh chưa chỉnh. Các recipe/LUT tạo biến thể phải có quyền sử dụng và phiên bản renderer; biến thể không tăng số người/cảnh độc lập.

Pilot đề xuất hiện hành: bắt đầu từ ảnh riêng hiện có, chọn mẫu duyệt cho 8–12 look; mở rộng tới khoảng 200 source và 600–1.000 targets khi đủ dữ liệu. Đây là kế hoạch thay cho các ước lượng pilot cũ, không phải số lượng đã có hoặc ngưỡng đủ để train tốt.

Gate B: role/group/color được duyệt, triplets có target, khóa holdout theo phiên/người và có look chưa học. Hiện triplets rỗng, training_enabled=false; quyền đã xác nhận không đồng nghĩa dữ liệu đã sẵn sàng.

## Công cụ và đầu ra

Chạy `Inventory.ps1`, sau đó `Prepare-CustomerPilot.py --inventory <manifest> --output <new-folder> --user-approved-commercial-training`. Flag chỉ dùng khi người dùng thực sự xác nhận. Tool đối chiếu lại SHA256 trước tạo preview, giữ source read-only, từ chối output tồn tại/trong source. Preview JPEG giảm kích thước và EXIF-orient, chưa ICC-normalized; không dùng làm target màu.

Batch đầu: `.local/matchlook-data/customer-inventory-001` và `.local/matchlook-data/customer-pilot-001`. Pilot có assets.json, phase-ab.json, summary.json và index.html. Phase A/B được thiết lập, chưa fine-tune hoặc đóng benchmark.

Kiểm tra thực tế: 176/176 ảnh decode và tạo preview thành công, không trùng SHA256. EXIF ghi X-H2 cho cả batch, 146 ảnh 5472×3648 và 30 ảnh 7728×5152, EXIF ColorSpace=1. Đây là metadata, không chứng minh ảnh chưa chỉnh hoặc film simulation nào. Tool đối chiếu lại hashes nguồn khớp inventory. Đã kiểm tra đủ 176 previews, quyền user-confirmed, split unassigned và training_ready=false.
