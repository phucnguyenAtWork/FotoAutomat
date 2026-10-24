# Ảnh mới CustomerImages — kiểm kê 12/10/2026

## Kết quả đọc thư mục

`D:/MatchLook/CustomerImages` có192file JPEG. Đối chiếu SHA256 với176assets của phase-b-pilot-001:

-176hash cũ còn nguyên, không thiếu/thay đổi.
-16file có nội dung mới, không có hash trùng nhau trong192file.
-16/16JPEG giải mã được. Tất cả EXIF ghi Fujifilm X-H2; chưa có RAW hay file liveview trong thư mục này.
-Không cập nhật dataset/split85/15 cũ; không train, không tạo profile, không chép ảnh ra dịch vụ bên ngoài.

Ảnh mới: DSCF0185/0186/0289/0290, DSCF2709/2710/2737/2738/2777/2778/2830/2831/2884/2885/2927/2928.JPG.

## Metadata và quan sát

4ảnh đầu5472x3648;4ảnh7728x5152;8ảnh5152x5152. EXIF aperture đều f/3.2, nhưng ISO640/800/2500, tốc độ1/100,1/80,1/60, WB/exposure-mode/flash flags khác nhau. Chưa thể coi đây là một cấu hình khóa thống nhất; EXIF không chứng minh công suất/nguồn đèn hoặc quá trình chỉnh ảnh. Ngày4ảnh đầu ghi2022-01-01 trong khi12ảnh sau ghi2026-10-12: không tự gán session từ timestamp.

Đã xem contact sheet16ảnh: có chân dung1người,2người, nhóm đông người, da sáng, nền sáng và2ảnh có vẻ thiên monochrome/tint. Đây là quan sát hình ảnh, chưa phải nhãn phong cách hoặc nhãn clip. Các cặp liên tiếp giống bố cục có nguy cơ leakage nếu chia ngẫu nhiên; exact hash khác không chứng minh nội dung độc lập. Chưa gán split, cần giữ cụm cùng buổi/burst/nhân vật khi thiết kế đánh giá.

## Vai trò đã xác nhận bởi người dùng

Có thể dùng bổ sung thử nghiệm Match Look visual: đa dạng số mặt, độ sáng, tông màu/monochrome. Chưa có ground truth đầu ra, bảng màu, cấu hình đèn, RAW+JPEG hoặc paired liveview nên chưa dùng để fit profile hoặc xác định WB/exposure đúng. Không suy luận filecamera/software EXIF là ảnh chưa chỉnh.

Người dùng xác nhận cả16ảnh là JPEG xuất thẳng từ camera, không chỉnh màu hậu kỳ; dùng làm source thử Match Look và cho phép training. Đã ghi source_role=camera_rendered_jpeg, source_role_review=user_confirmed, training_use_authorized=true trong source-manifest.confirmed.json. Camera vẫn render WB/film simulation/tone curves vào JPEG; không coi đây là ảnh RAW trung tính. Quyền training không tự tạo target ground truth hoặc chứng minh cấu hình booth khóa. Không suy ra quyền tải công khai.

## Artifacts local

`.local/matchlook-data/customer-additions-2026-10-12-001/report.json`:192file inventory summary +16newasset metadata/hash, mỗi asset training_readyfalse. `overview.jpg` đã xem; `index.html` chứa thumbnail16ảnh có nhãn tiếng Việt, chưa browser QA. Script kiểm kê local `.local/audit-customer-additions.py`, nguồn ảnh không sửa.

Bước tiếp theo: grouping session/nhân vật/near-duplicate rồi gán split, chuẩn bị augmentation/target hợp lệ cho nguồn camera JPEG. Giữ split cũ, không dùng hai ảnh khác nội dung bất kỳ làm paired ground truth. Nếu chạy frozen SSL visual, ghi development exposure để không coi các ảnh đó là test mù. Chưa fine-tune; thông báo người dùng trước khi bắt đầu.
