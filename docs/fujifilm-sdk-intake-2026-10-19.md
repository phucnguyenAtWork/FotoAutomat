# Kiểm kê Fujifilm SDK do người dùng cung cấp

Ngày kiểm tra: 19/10/2026. Chỉ đọc tệp và tài liệu web; không nạp DLL, không chạy sample, không mở camera hoặc chụp.

## Gói cục bộ

Thư mục gốc thực tế: `C:\Users\Admin\Downloads\SDK13410\SDK13410`.

- `REDISTRIBUTABLES/Windows/64bit`: XAPI.dll, XSDK.DAT, FTLPTP.dll, FTLPTPIP.dll và các module FF0000API.dll … FF0022API.dll.
- Có thư mục Windows 32bit riêng; dự án booth ưu tiên x64.
- `HEADERS/XAPI.H`, `HEADERS/X-H2.h` cùng các header theo model.
- `MANUAL/SDK_ProgrammingReference.pdf` tồn tại. Chưa trích xuất/đọc toàn bộ PDF: môi trường hiện kiểm tra chưa có pdftotext, pypdf, pdfplumber hoặc PyMuPDF. Không coi kiểm kê header là đã đọc toàn bộ manual.
- Sample Windows có C# (ZoomPos), C++ (HotFolder, ReleaseButton…), Python (LiveView).
- FileVersion/ProductVersion của XAPI.dll x64 là `1.17.0.0`. Đây là version thành phần, không suy ra version toàn gói từ tên thư mục SDK13410.

## API đã đối chiếu header

- Vòng đời: XSDK_Init / XSDK_Exit.
- Tìm và mở camera: XSDK_Detect / XSDK_OpenEx / XSDK_Close.
- Thông tin và capability: XSDK_GetDeviceInfo / XSDK_GetDeviceInfoEx.
- Chụp: XSDK_Release / XSDK_ReleaseEx.
- Nhận ảnh: XSDK_ReadImageInfo / XSDK_ReadImage.
- Có XSDK_DeleteImage: cần đọc chính xác semantics trong manual trước khi triển khai, không tự gọi để dọn dữ liệu camera.

Sample `SAMPLES/Windows/LiveView/live_view_control.py` có thao tác đổi priority và chế độ ghi media RAW+JPEG ngay lúc kết nối. Vì vậy không chạy nguyên sample để “kiểm tra kết nối”, không sao chép các thay đổi thông số đó vào adapter mặc định.

## Điều khoản đã đọc

Nguồn: [EULA cá nhân chính thức](https://www.fujifilm-x.com/global/camera-control-sdk/agreement/), đọc 19/10/2026. Không tìm thấy file EULA riêng bằng tên trong gói vừa kiểm kê.

Điều 2 cho phép phát triển và phân phối thư viện dạng object code khi tích hợp vào ứng dụng, kèm nghĩa vụ với khách hàng. Không đồng nghĩa được phân phối toàn bộ SDK. Điều 5.2–5.3 quy định ngoại lệ bảo hành và nghĩa vụ thông báo khách hàng. Đây là căn cứ để chuẩn bị tích hợp, chưa phải xác nhận toàn bộ quy trình phát hành đã tuân thủ.

## Bước triển khai tiếp theo

1. Đọc manual về ABI, threading, polling live view, truyền ảnh và lifetime/buffer ownership.
2. Viết adapter Infrastructure nạp DLL từ đường dẫn cục bộ; không commit SDK hoặc sample của hãng.
3. Kiểm tra load/version không mở camera, rồi discovery và đọc capability khi có thiết bị thử.
4. Nối camera settings với giá trị/capability đọc thật; chỉ bật nút được hỗ trợ.
5. Kiểm thử live view và shutter/download sau khi được phép thử phần cứng. Không tự đổi WB/exposure/media của camera từ sample.

Adapter Fujifilm vẫn chưa được triển khai trong lượt kiểm kê này. Không có kết quả phần cứng hoặc xác nhận tương thích runtime.
