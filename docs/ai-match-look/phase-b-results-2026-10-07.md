# Phase B — controlled dataset pilot, 07/10/2026

Đã tạo `.local/matchlook-data/phase-b-pilot-001`: 176 ảnh PNG xem thử, cạnh dài tối đa 960 px, EXIF orientation và sRGB theo ICC/EXIF, đối chiếu SHA256 ảnh đầu vào. Giữ nguyên originals. Chọn 12 source trải đều ba nhóm, tạo 96 triplets với 8 phép biến đổi: neutral, warm, cool, pastel, film matte, high contrast, low key, monochrome. Reference dùng ảnh khác source; target giữ hình học source.

Người dùng xác nhận trong chat ngày 07/10: ba dải DSCF3948–4037 (90 ảnh), DSCF4410–4465 (56), DSCF7889–7918 (30) là ba buổi chụp khác nhau; ảnh đã có preset. Đã cập nhật dataset/summary và hash liên kết trang review. Công cụ Build mặc định vẫn tạo trạng thái pending cho batch mới; không tự áp xác nhận này cho dữ liệu khác.

## Ý nghĩa dữ liệu

Source là JPEG đã chỉnh, không phải RAW hay JPEG trung tính. Tên và thông số preset sẵn có chưa biết. `neutral` là phép giữ nguyên pixels, không khử preset. Áp cùng công thức bổ sung lên hai ảnh có preset nền khác nhau không bảo đảm cùng look tuyệt đối. Bộ này kiểm tra phép chuyển màu có kiểm soát; chưa là ground truth về màu máy ảnh hoặc bộ phong cách khách hàng đã duyệt. Không dùng model sinh target rồi coi là chuẩn độc lập.

Ba buổi đã xác nhận nhưng chưa kiểm tra người/cảnh trùng giữa buổi. Toàn batch vẫn chung leakage group và `diagnostic_only`; chưa chia train/test, chưa khóa holdout. Target thực tế cần được quản trị chỉnh/duyệt theo look mong muốn, có kiểm tra da và cảnh. Chưa có face masks, nhãn vùng da hoặc benchmark cảnh quan đại diện trong batch này.

## Duyệt bằng tay

Mở `index.html`, chọn look, so Source / Reference / Proposed target. Đánh dấu Phù hợp, Cần sửa hoặc Loại rồi bấm Xuất đánh giá JSON. Trạng thái trình duyệt gắn với hash manifest; JSON xuất không tự phê duyệt train. Chưa có importer hợp nhất đánh giá. `look-overview.jpg` là bảng tổng quan 8 look trên một source.

Không đánh giá độ giống CanonCGT từ các ảnh target này: chúng do renderer công thức tạo, chưa phải output inference. Renderer dùng phép toán encoded sRGB, không mô phỏng vật lý Exposure hay engine Adobe. Biến thể không làm tăng số cảnh/người độc lập.

## Kiểm chứng

- `Test-PhaseB.py`: 4 tests PASS (identity, monochrome/range, pending gate, parent/split leakage).
- `Validate-PhaseB.py <dataset.json>`: kiểm tra 368 artifacts về hash/decode/path containment và geometry source-target; cấu trúc hợp lệ.
- dHash phát hiện 0 ứng viên gần trùng ở ngưỡng đã đặt; không chứng minh không có ảnh tương tự.
- Đã xem bảng tổng quan, gồm cả monochrome và low-key. Chưa duyệt toàn bộ 96 target hoặc kiểm thử tương tác trang bằng trình duyệt.

Công cụ: `Build-PhaseB.py`, `PhaseBData.py`, `Validate-PhaseB.py`, `Test-PhaseB.py` trong `tools/MatchLookData`. Trạng thái: pilot có thể duyệt; `training_ready=false`, còn split, holdout và target review. Chưa fine-tune, chưa có phần trăm chất lượng model mới.
