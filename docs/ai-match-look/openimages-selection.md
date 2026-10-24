# Open Images: chọn ứng viên từ metadata

`tools/MatchLookData/Select-OpenImages.py` chỉ đọc bốn CSV người dùng tải tại `D:\MatchLook\OpenImages\metadata`. Dùng CSV streaming, không nạp toàn bộ file vào RAM, không tải ảnh và không sửa nguồn. CodeGraph hiện không trả về công cụ liên quan nên triển khai công cụ độc lập.

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Select-OpenImages.py --metadata D:/MatchLook/OpenImages/metadata --output .local/matchlook-data/openimages-selection-001
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Test-OpenImages.py
```

Output phải mới và ngoài metadata. Chỉ chọn split Train, license metadata CC BY 2.0, có tác giả/profile, URL ảnh và trang nguồn. Giữ nguyên thông tin để duyệt quyền; không tự cấp quyền train hay cho rằng có model release.

Mục tiêu 700 ảnh portrait: ít nhất một face box chiếm >=4% diện tích ảnh, chiều ngang/dọc >=12%, flags occluded/truncated/group/depiction đều bằng 0; bỏ ứng viên portrait có nhãn Human face phủ định. Không bắt buộc có thêm nhãn Human face cấp ảnh vì nhãn này thưa hơn boxes. Đây là heuristic chọn mặt đủ lớn tương đối, chưa biết độ nét hoặc kích thước pixel.

Mục tiêu 300 ảnh cảnh: mỗi lớp Room/Landscape/Mountain/Forest/Beach/Lake 50 ảnh với nhãn dương tính. Các lớp có thể giao nhau hoặc có người. Chọn portrait trước, sau đó cảnh theo thứ tự trên, không trùng ImageID. Thứ tự chọn xác định bằng SHA256 của seed cố định + ImageID; tối đa 5 ảnh/tác giả trên toàn tập. Nếu thiếu quota, báo số thực tế, không tự nới điều kiện.

Output: `review.csv`, `candidates.json`, `image-ids.txt` dùng định dạng downloader upstream và `summary.json`. Tất cả ứng viên vẫn `training_ready=false`. Chưa xem pixels, lọc watermark/nudity, xác minh tuổi/người đồng ý, cân bằng tông da, nhận diện trùng nội dung giữa các ID hoặc tạo target chỉnh màu. Không suy ra danh tính từ khuôn mặt. Cần duyệt ảnh và quyền trước train; giữ nguồn Train không thay thế chia nội bộ theo phiên/người cho Match Look.

Kiểm thử synthetic: face geometry/flags, loại nhãn âm, loại NC, giữ trạng thái pending, từ chối ghi đè hoặc output trong nguồn.

Kết quả 06/10/2026: `.local/matchlook-data/openimages-selection-002/` chứa 974 ứng viên duy nhất: 700 portrait, Room 50, Landscape 24, Mountain/Forest/Beach/Lake mỗi nhóm 50. 842 tác giả, tối đa 5 ảnh/tác giả. Landscape chỉ có 24 ứng viên phù hợp trong phần metadata boxable nên thiếu 26 so với quota; không bù ngầm. Hai tests synthetic PASS. Lượt 001 là thử nghiệm điều kiện nhãn mặt kép, không dùng làm danh sách hiện hành. Chưa tải ảnh hoặc train.

## Tải và duyệt ảnh

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Download-OpenImages.py --selection .local/matchlook-data/openimages-selection-002/candidates.json --output .local/matchlook-data/openimages-review-001
```

Downloader dùng HTTPS tới bucket `open-images-dataset` cùng nguồn script Open Images chính thức, 5 luồng, retry hữu hạn, không tắt TLS. Output phải mới; file ảnh chỉ được đổi tên từ `.part` sau khi decode thành công. Lỗi được giữ trong manifest, không tự thay bằng ảnh khác. JSONL được flush từng kết quả để phục hồi thông tin nếu gián đoạn; chưa hỗ trợ resume tự động.

Giữ byte tải từ CVDF nguyên bản trong `images/`; không gọi đây là Flickr originals. OriginalMD5 metadata không dùng đối chiếu bản CVDF vì có thể khác độ phân giải. Ghi SHA256 cục bộ, kích thước, ICC presence và nguồn cho mỗi ảnh. Thumbnails JPEG chỉ resize/EXIF orient, không chuẩn hóa ICC; không dùng làm ground truth màu.

`index.html` và `review-XX.html`: gallery chia trang 48 ảnh, link ảnh lớn, tác giả, nguồn và license. `contact-sheet-XX.jpg`: bảng ảnh thu nhỏ có số thứ tự/ID để trao đổi. `manifest.json` giữ metadata và trạng thái; `summary.json` ghi số thành công/thất bại. Chưa kiểm định màu, quyền chân dung hoặc tính phù hợp từng ảnh; `training_ready=false`.

Kết quả tải 06/10/2026 tại `.local/matchlook-data/openimages-review-001`: 974/974 thành công, 0 lỗi, 277.243.511 bytes ảnh (~277 MB), 974 SHA256 khác nhau. Đã đối chiếu số file và byte size với manifest. 21 trang HTML và 21 contact sheets; sheets đã kiểm tra chiều cao đủ số hàng. Cạnh ngắn nhỏ nhất 284 px, vì vậy không xem bộ này là full-resolution. Xem trực quan sheet đầu thấy ảnh thiếu sáng, đen trắng, sinh hoạt và trẻ em; chưa duyệt toàn bộ, chưa tự đánh dấu ảnh nào training-ready. Bước tiếp là review chất lượng/quyền và lựa chọn mẫu phù hợp booth, không train ngay.
