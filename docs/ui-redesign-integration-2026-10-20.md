# Tích hợp redesign — 20/10/2026

Đã áp 6 XAML trang/control từ thư mục 2026-10-20-ui-redesign vào Desktop: LayoutWindow, GeneralSettingsPanel, PrinterSettingsPanel, CaptureModesPanel, ManagementPanel, CompactColorControls; thêm Theme, ThemeExtras, AdminDefaults và hai partial UiState. Bản trước được giữ tại .local/ui-redesign-backup-2026-10-20. Không xóa file nguồn import.

Đã sửa root resource PageBrush/TextBrush sang DynamicResource trên các UserControl mới để tránh lỗi StaticResource khi khởi tạo. Rút gọn chú thích xuất tệp trong cài đặt chung.

Camera UI import trước được liên kết vào Desktop. Tham số --ui-preview mở quản trị và chọn camera preview dùng PreviewServices mô phỏng, lưu camera chỉ trong bộ nhớ. Không có tham số này, trang camera Fujifilm SDK hiện tại vẫn hoạt động như trước. Các trang quản trị khác vẫn dùng store hiện có, không phải sandbox cấu hình. Không thay backend chụp/in hoặc gọi phần cứng trong đợt kiểm tra này.

Build solution Release: 0 lỗi, 0 cảnh báo ở lượt cuối. 166/166 test đạt (trước sửa resource XAML cuối; không thay logic shared sau đó). Đã mở và xem trực tiếp camera, cài đặt chung, máy in, chế độ chụp, bộ lọc màu. Chưa kiểm tra toàn bộ thao tác lưu/hoàn tác, DataGrid, DPI, màn hình nhỏ hay phần cứng.

Bản ghép: .local/builds/ui-redesign-2026-10-20. Chạy Open-Preview.cmd để mở camera preview trong quản trị. Chạy exe với --admin để dùng trang SDK hiện tại.

## Đợt 2 — đã ghép 20/10/2026

- Nhập NumericStepper, ThemeControls, CaptureModesPanel (XAML/C#), FrameLibraryPanel (XAML/C#), LayoutWindow.FrameLibrary.cs và LayoutWindow.xaml từ bộ file 13:45.
- IsLayout chỉ còn đúng với Bố cục in. Bản XAML mới bật FrameLibraryPanel bằng DataTrigger trực tiếp trên Section; không cần thêm IsFrameLibrary hoặc Changed riêng.
- Giữ DynamicResource cho PageBrush/TextBrush ở gốc hai panel để tránh lỗi tra tài nguyên lúc khởi tạo.
- Bỏ khối cài đặt chụp dự phòng theo bản nhập; thời gian phiên nằm trong CaptureModesPanel. Không xóa các thuộc tính/lệnh cũ của ViewModel trong đợt này.
- Sao lưu các file bị thay vào .local/ui-redesign-round2-backup.

### Kiểm chứng

- Build Desktop Release và bản riêng .local/builds/ui-redesign-round2-2026-10-20: 0 lỗi, 0 cảnh báo.
- Đã chạy WPF --ui-preview trên Windows. Hàng chip, segmented tốc độ phát/kích thước hiển thị đúng ở cửa sổ 1366 × 843.
- 360°: thêm đoạn thứ hai hiển thị đúng, Hoàn tác phục hồi một đoạn; không lưu thay đổi cấu hình.
- Thư viện hiển thị 4 khung hiện có; Sửa mở đúng bố cục; Xóa mở xác nhận và chọn No, không xóa file. Chưa chạy nhánh xóa thực tế hoặc Dùng cho khách.
- Hàng thumbnail cao 168: nhãn hiện đủ ở kích thước cửa sổ đã kiểm tra. Chưa kiểm toàn bộ DPI/kích thước nhỏ.
- Không kiểm thử thiết bị hoặc pipeline video; đây là ghép UI. Camera trong --ui-preview vẫn mô phỏng.

## Kiểm tra tiếp và tùy chọn README

- Đồng bộ WorkspaceToolsPanel về lề 24, tiêu đề 24 semibold; EditorUi dùng Accent #0F766E.
- CameraPage dùng ThemeExtras và SaveBarStyle; thêm resource liên kết vào project CameraUiPreview để preview độc lập vẫn build được.
- dotnet format FotoAutomat.slnx --verify-no-changes --no-restore: exit 2, tồn tại lỗi WHITESPACE ngoài phạm vi UI (log .local/format-round2.log). Không format toàn repo.
- Đã format và verify 8 file C# UI liên quan (NumericStepper, CaptureModesPanel, FrameLibraryPanel, LayoutWindow.FrameLibrary, GeneralSettingsPanel.UiState, PrinterSettingsPanel.UiState, WorkspaceToolsPanel, EditorUi): exit 0.
- Build solution vào bin thường bị khóa exe bởi ứng dụng đang mở. Build toàn solution với OutputPath riêng .local/builds/ui-redesign-followup-2026-10-20: 0 lỗi, 0 cảnh báo. CameraUiPreview độc lập: 0 lỗi, 0 cảnh báo.
- Các thay đổi tùy chọn lượt này chưa được xem lại trong runtime; cửa sổ đang mở chưa tự nhận code mới.
- Chưa có bảng đổi Group 7 → 5 trong README/brief tìm được; giữ nguyên Sections chờ bảng ánh xạ.
- Giữ các chú thích trong danh sách chờ cho phép xóa; nhãn Loại tệp xuất đã được rút gọn ở lượt trước.

## Menu 5 nhóm — đã áp dụng bảng ánh xạ

Thay Groups và thứ tự 15 phần tử Sections theo 2026-10-20-ui-redesign/2026-10-20-menu-5-nhom.md: TỔNG QUAN → CHỤP ẢNH → BỘ MÀU → KHUNG & IN → VẬN HÀNH. Đã đối chiếu bằng script: giữ nguyên đủ 15 Title và Description. IsLayout vẫn chỉ áp dụng Bố cục in. Không đổi routing hoặc xóa chú thích.

Build toàn solution ra .local/builds/menu-five-groups-2026-10-20: 0 lỗi, 0 cảnh báo. Chưa kiểm tra runtime cho thứ tự menu mới; ứng dụng đang mở không tự cập nhật. Tình trạng format toàn repo chưa đạt từ lượt trước vẫn giữ nguyên.

## Đợt 3 — đã ghép 20/10/2026

- Nhập từ 2026-10-20-ui-redesign: PrinterSettingsPanel (XAML, UiState), LayoutWindow.xaml, LayoutWindow.FrameLibrary.cs và 7 file Bộ lọc màu (EditorUi, ColorEditorPanel, ColorAdvancedControls, MatchLookTool, RetouchEditorPanel, RefineEditorPanel, WorkspaceToolsPanel). Đã đối chiếu diff: 7 file Bộ lọc màu chỉ khác src ở các dòng trong 2026-10-20-bo-loc-mau-thay-doi.md, nên thay nguyên file.
- Bản nhập PrinterSettingsPanel.xaml quay lại StaticResource PageBrush/TextBrush ở gốc và làm cửa sổ quản trị không mở được (XamlParseException "Cannot find resource named 'PageBrush'"). Đã đặt lại DynamicResource.
- Không dùng DefaultPagePanel (trang chỉ có tên + mô tả). IsGeneralSettings giữ nguyên ba mục; GeneralSettingsPanel chia trang theo Section bằng DataTrigger, tiêu đề lấy theo Section:
  - Cài đặt chung: Hiển thị (toàn màn hình, tăng tốc đồ họa), Kết nối (API điều khiển booth), Thư mục.
  - Màn hình & thương hiệu: Thương hiệu (tên, logo), Xem trước, Kích hoạt thương hiệu.
  - Quyền quản trị: Bảo mật (PIN), không có thanh lưu chung.
- Thiếu PIN (lần chạy đầu, Khởi động giao diện booth) chuyển sang Quyền quản trị thay vì Cài đặt chung.
- GeneralSettings thêm LogoFile (chỉ tên tệp). GeneralSettingsStore.ImportLogo chép PNG/JPEG ≤ 5 MB vào thư mục Operator với tên mới khi bấm Lưu; Save xóa logo cũ sau khi ghi thành công. general.json cũ (không có LogoFile) vẫn đọc được. Logo nằm trong trạng thái chưa lưu/Hoàn tác.
- MainWindow: header, tiêu đề màn hình chào và dòng chân trang dùng logo hoặc tên thương hiệu; header vẫn ẩn ở màn hình chào như trước. Tiêu đề màn hình chào khi không có logo là tên thương hiệu in đậm một kiểu, không còn "Foto" nghiêng + "Hunter" đậm.
- Sao lưu các file bị thay vào .local/ui-redesign-backup-dot3-2026-10-20.

### Kiểm chứng

- Build solution Debug: 0 lỗi, 0 cảnh báo. 169/169 test đạt (thêm 3 test logo trong GeneralSettingsTests).
- Mở và chụp cả 15 mục quản trị từ bản sao của src (dùng --render-preview): không lỗi khởi động. Đã xem Bộ lọc màu, Bố cục in, Máy in, Căn chỉnh bản in, Cài đặt chung, Màn hình & thương hiệu, Quyền quản trị; màn hình chào của khách có và không có logo. Dữ liệu thử dùng thư mục Operator tạm, không ghi vào %LOCALAPPDATA%.
- Chưa thử thao tác thật: chọn logo bằng hộp thoại, Lưu/Hoàn tác, đổi PIN; header có logo ở các màn sau màn hình chào; DPI và màn hình nhỏ.
- dotnet format: lỗi CRLF/WHITESPACE có sẵn trong repo vẫn còn; file mới (BrandImage.cs) và GeneralSettingsStore.cs không có lỗi.

## Hộp mở khóa PIN dạng bàn phím số — 20/10/2026

- PinWindow thay ô PasswordBox bằng bàn phím cảm ứng kiểu ATM theo màu giao diện khách: tiêu đề "Nhập mã PIN", hàng chấm (tối thiểu 6, tối đa 12), dòng lỗi, phím 1–9, ⌫ (xóa một số), 0, ✓ (mở khóa, bật khi đủ 6 số) và nút Hủy. Bàn phím vật lý: số, Backspace, Enter, Esc. Sai PIN hoặc đang bị khóa 30 giây thì xóa số đã nhập và hiện lỗi.
- Cửa sổ không viền, bo góc, đổ bóng, căn giữa cửa sổ khách. Kiểm tra logic PIN (GeneralSettingsStore.VerifyPin) không đổi.
- Đã chụp trạng thái trống và đã nhập 7 số từ bản sao. Chưa thử chạm thật trên màn cảm ứng, sai PIN 5 lần và DPI khác.

## Máy in & căn chỉnh gọn — 20/10/2026

- PrinterSettingsPanel viết lại theo bố cục tham khảo: một trang cho cả hai mục menu Máy in và Căn chỉnh bản in. Bỏ ô chọn "Máy in chính/phụ"; hai máy hiện song song (chọn hàng đợi Windows + Thông số driver) và căn chỉnh cả hai máy cùng lúc.
- Số tờ, giới hạn sự kiện/phiên đổi từ TextBox sang NumericStepper. Icon dạng Path (vector nét 24 × 24) trong tài nguyên của panel; không tooltip.
- Không thêm mục không có trong PrinterConfiguration (P3/P4, xoay, độ nét, in thử).
- Bỏ mục menu "Căn chỉnh bản in" (Sections còn 14 mục); IsPrinterSettings chỉ còn "Máy in".
- Sao lưu bản trước tại .local/printer-compact-backup-2026-10-20. Build Debug 0 lỗi 0 cảnh báo, 169/169 test, format các file C# đã sửa đạt. Đã chụp cả hai mục từ bản sao. Chưa thử chọn máy in, mở PrintDialog, Lưu/Hoàn tác bằng thao tác thật.

## Đợt 4 và giao diện khách theo thương hiệu — 20/10/2026

- Áp dụng đợt 4 (bảng giá Khổ nhỏ/Khổ lớn, màn Chọn khổ in) từ 2026-10-20-ui-redesign/2026-10-20-dot-4 sau khi đối chiếu diff: các file được làm từ src hiện tại, giữ phần logo. Sao lưu tại .local/dot4-backup-2026-10-20.
- NumericStepper hiển thị số theo vi-VN (90.000 đ, 0,5 mm) bất kể ngôn ngữ Windows.
- Một key = một thương hiệu cho bản cài; chức năng như nhau. GeneralSettings thêm AccentColor, BackgroundColor, BackgroundFile (có mặc định, general.json cũ vẫn đọc được). BrandColors (Core) suy ra các màu phụ từ màu nhấn và kiểm tra tương phản; màu mặc định giữ đúng bảng màu Foto Hunter.
- Trang Màn hình & thương hiệu: thẻ Giao diện khách (ô màu mẫu + mã #RRGGBB cho màu nhấn và màu nền, ảnh nền chọn/xóa) và khung Xem trước cập nhật ngay. Không tạo BrandPanel riêng như kế hoạch của Claude chat, vì tên và logo đã nằm ở trang này.
- MainWindow đổi màu Rose/ActionRose/Wine/Blush tại chỗ theo màu nhấn, vầng sáng màn chào dùng DynamicResource BrandGlowColor, nền dùng màu nền và ảnh nền (lớp phủ 55%); màn Chụp giữ nền tối. Màu Line (chấm tiến trình chưa tới, viền thẻ) và Cream (chữ trên màn Chụp) không đổi theo thương hiệu.
- Kiểm chứng: build Debug 0 lỗi 0 cảnh báo, 183/183 test, harness 4/4, format các file C# mới/sửa đạt. Đã chụp màn chào, Chọn khổ và trang quản trị với thương hiệu thử (teal + ảnh nền) và với mặc định. Chưa thử chọn màu/ảnh bằng thao tác thật, Lưu/Hoàn tác, các màn sau thanh toán và màn Chụp.
