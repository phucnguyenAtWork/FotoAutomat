# FotoAutomat: giao diện quản trị mới (2026-10-20)

## Trạng thái kiểm tra
- Chưa biên dịch và chưa chạy (môi trường này không có .NET, WPF). Đã kiểm tra: XML hợp lệ, mọi `StaticResource` có khóa, mọi `x:Name`, handler và binding của bản gốc còn đủ trong bản mới.
- Đợt 2: file C# mới đã qua bộ phân tích cú pháp C# (không có lỗi cú pháp); chưa kiểm tra kiểu và chưa biên dịch. Với `TreatWarningsAsErrors`, cảnh báo của analyzer có thể làm build đỏ ở các file mới.
- Cần chạy `dotnet build`, `dotnet format --verify-no-changes` và xem bằng mắt trên Windows trước khi dùng.

## File và vị trí áp dụng
Tất cả nằm trong `2026-10-20-ui-redesign/`. Chưa file nào trong `src/` bị sửa.

| File mới | Việc cần làm | Gốc bị thay |
|---|---|---|
| `2026-10-20-ThemeExtras.xaml` | Thêm vào `src/FotoAutomat.Desktop/`, giữ tên | file mới |
| `2026-10-20-AdminDefaults.xaml` | Như trên, giữ tên | file mới |
| `2026-10-20-GeneralSettingsPanel.xaml` | Thay `GeneralSettingsPanel.xaml` | có |
| `2026-10-20-GeneralSettingsPanel.UiState.cs` | Thêm, đổi tên thành `GeneralSettingsPanel.UiState.cs` | file mới |
| `2026-10-20-PrinterSettingsPanel.xaml` | Thay `PrinterSettingsPanel.xaml` | có |
| `2026-10-20-PrinterSettingsPanel.UiState.cs` | Thêm, đổi tên thành `PrinterSettingsPanel.UiState.cs` | file mới |
| `2026-10-20-CaptureModesPanel.xaml` | Thay `CaptureModesPanel.xaml` (bản gọn, đợt 2) | có |
| `2026-10-20-CaptureModesPanel.xaml.cs` | Thay `CaptureModesPanel.xaml.cs` (bản gọn, đợt 2) | có |
| `2026-10-20-NumericStepper.xaml` và `.xaml.cs` | Thêm, đổi tên thành `NumericStepper.xaml`, `NumericStepper.xaml.cs` | file mới |
| `2026-10-20-ThemeControls.xaml` | Thêm vào `src/FotoAutomat.Desktop/`, giữ tên | file mới |
| `2026-10-20-FrameLibraryPanel.xaml` và `.xaml.cs` | Thêm, đổi tên thành `FrameLibraryPanel.xaml`, `FrameLibraryPanel.xaml.cs` | file mới |
| `2026-10-20-LayoutWindow.FrameLibrary.cs` | Thêm, đổi tên thành `LayoutWindow.FrameLibrary.cs` | file mới |
| `2026-10-20-ManagementPanel.xaml` | Thay `ManagementPanel.xaml` | có |
| `2026-10-20-LayoutWindow.xaml` | Thay `LayoutWindow.xaml` | có |
| `2026-10-20-CompactColorControls.xaml` | Thay `CompactColorControls.xaml` | có |

- `2026-10-20-Theme.xaml` (đã có ở thư mục gốc repo) cũng phải nằm cùng thư mục với hai file theme mới. Các file XAML tham chiếu nhau bằng tên có tiền tố ngày nên không đổi tên file theme.
- Các file `.xaml.cs` gốc giữ nguyên. Không để cả file gốc lẫn file mới cùng `x:Class`.
- Thứ tự gợi ý: theme, rồi `GeneralSettingsPanel`, build, xem; sau đó từng trang còn lại; `LayoutWindow` và `CompactColorControls` cuối cùng vì ảnh hưởng toàn cửa sổ.

## Đợt 2: Thư viện khung và Cài đặt chế độ chụp gọn
Cần sửa đúng một chỗ trong file gốc (tôi không sửa `src/`). Tìm theo chuỗi:

`src/FotoAutomat.Desktop/ViewModels/LayoutEditorViewModel.cs`
- Tìm `public bool IsLayout => Section is "Bố cục in" or "Thư viện khung";`
- Thay bằng `public bool IsLayout => Section is "Bố cục in";`

Không làm bước này thì trang Thư viện khung vẫn hiện, nhưng trình chỉnh Bố cục in vẫn ở trạng thái hoạt động bên dưới; phím tắt của trình chỉnh (`OnEditorKeyDown`) có thể vẫn tác động lên ô đang chọn (suy đoán, chưa kiểm tra).

Thư viện khung (`FrameLibraryPanel`):
- Hiện các mẫu `.fotolayout` đã lưu từ Bố cục in, không hiện 10 preset có sẵn. Mỗi thẻ có thumbnail, tên, khổ giấy và số ô, badge "Đang dùng" cho mẫu trùng `active.fotolayout`.
- Dùng cho khách: ghi mẫu vào `PrintLayoutStore.ActivePath` (cùng thao tác với nút dùng mẫu trong Bố cục in).
- Sửa: chuyển sang Bố cục in và mở mẫu (hỏi lưu nếu trình chỉnh đang có thay đổi chưa lưu).
- Xóa: xóa file mẫu sau khi xác nhận. Không đụng `active.fotolayout`.
- Tạo khung mới: chuyển sang Bố cục in.
- Panel đọc lại thư viện mỗi lần trang được mở.

Cài đặt chế độ chụp (bản gọn):
- Thẻ "Phiên chụp" luôn hiện: đếm ngược trước mỗi ảnh, số ảnh chụp, xem lại ảnh, thời gian phiên. Ba mục đầu trước đây chỉ hiện ở Ảnh chụp và Ảnh thẻ.
- Thời gian phiên chuyển sang đây. Khối cũ trong `LayoutWindow.xaml` (Đếm ngược, Thời gian phiên, Xem ảnh, Lưu cấu hình chụp, Hủy thay đổi) bị che sau panel từ trước nên tôi đã bỏ khỏi `2026-10-20-LayoutWindow.xaml`. Các thuộc tính và lệnh tương ứng trong `LayoutEditorViewModel` vẫn còn, chưa còn nơi dùng.
- Danh sách chế độ chuyển thành hàng chip, chấm xanh cho chế độ đang bật.
- Slider thay bằng NumericStepper (− giá trị +, giữ nút để lặp). Bước nhảy: chất lượng 5, khoảng cách khung hình 50 ms, còn lại 1. Giá trị bị chặn trong khoảng hợp lệ như `Validate()`.
- Kích thước, xoay và tốc độ phát chuyển thành danh sách phân đoạn.
- Một vùng cuộn cho cả trang (bỏ vùng cuộn lồng nhau).
- Không tooltip. Nút chọn tệp hiện tên tệp thay cho tooltip đường dẫn cũ.
- Mô hình dữ liệu và `OnSave`/`OnRevert` giữ nguyên. Các mục của FolaCapture không có trong model (Thủ công/Tự động, số lần hủy, thời gian chờ bắt đầu) không được thêm.

Sửa kèm trong `LayoutWindow.xaml`: hàng thumbnail Bố cục in cao 168 (trước 148) để nhãn không bị cắt. Nguyên nhân tôi đoán là chiều cao hàng gồm cả thanh cuộn ngang; cần xem lại bằng mắt.

## Cách hoạt động
- `LayoutWindow` nạp `AdminDefaults`, nên Button, TextBox, ComboBox, CheckBox, Slider, ScrollBar, Expander, TabControl, DataGrid trong cửa sổ quản trị đều có kiểu mới. Giao diện khách (`MainWindow`) không bị ảnh hưởng.
- Bộ lọc màu (Adjust, Retouch, Refine) đổi giao diện qua các style ngầm định và `CompactColorControls.xaml`. Không sửa `ColorGradingWheel`, `CurveEditor`, `LevelsEditor`, `ColorHistogram`, `MatchLookTool` hay `ColorEditor*`/`Retouch*`/`Refine*`.
- Slider mặc định không snap (giống slider gốc của cửa sổ). Trang Chế độ chụp không còn dùng slider từ đợt 2.
- Cỡ điều khiển: 40 px (Theme trang Máy ảnh là 36 px). Vùng chạm của công tắc 44 px, slider 40 px.

## Khác biệt hành vi so với bản gốc
- Cài đặt chung, Cài đặt in: thêm Hoàn tác và chỉ báo "Có thay đổi chưa lưu". Hoàn tác đọc lại từ store. Chỉ báo của Cài đặt in cập nhật sau khi ô nhập rời focus (binding mặc định LostFocus).
- Cài đặt in: ô số đặt cạnh nhãn, slider bên dưới; slider vẫn không snap.
- Chế độ chụp: `State` chuyển xuống thanh lưu; công tắc thay checkbox.
- Quản lý: thanh dưới chỉ hiện khi có thông báo hoặc ở mục Bảng giá. `Lưu bảng giá` nằm ở thanh này.
- "API điều khiển booth · Chưa kết nối" và "Kích hoạt thương hiệu · Chưa kết nối" tách thành nhãn + huy hiệu. Nội dung chữ không đổi.
- Bố cục in: màu chọn ô/tay nắm chuyển sang màu nhấn; hàng đệm trống 28 px dưới canvas bỏ (0).

## Thay đổi nhỏ tùy chọn (chưa áp dụng)
Tìm theo chuỗi, không theo số dòng.

`CameraPage.xaml` (dùng thanh lưu chung):
- Tìm `<ResourceDictionary Source="2026-10-20-Theme.xaml"/>`, thay bằng `<ResourceDictionary Source="2026-10-20-ThemeExtras.xaml"/>`.
- Tìm thẻ mở `<Border Grid.Row="3" Background="{StaticResource SurfaceBrush}"` kèm dòng ngay sau nó `BorderBrush="{StaticResource LineBrush}" BorderThickness="0,1,0,0" Padding="24,12">`, thay cả hai bằng `<Border Grid.Row="3" Style="{StaticResource SaveBarStyle}">`.

`WorkspaceToolsPanel.cs` (cùng lề 24 và cỡ tiêu đề với các trang khác):
- `new Thickness(28, 20, 28, 12)` (trong `var page = new Grid`) thành `new Thickness(24, 24, 24, 12)`.
- `new TextBlock { Text = section, FontSize = 26, Margin = new Thickness(0, 0, 0, 12) }` thành `new TextBlock { Text = section, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) }`.
- `private readonly StackPanel body = new() { Margin = new Thickness(28, 20, 28, 20) };` thành `new Thickness(24, 24, 24, 20)`.

Đồng bộ màu nhấn teal cũ `#477B79` sang `#0F766E`:
- `EditorUi.cs`: `Accent = Frozen(0x47, 0x7B, 0x79)` thành `Frozen(0x0F, 0x76, 0x6E)`.
- `CaptureModesPanel.xaml.cs`: `Color.FromRgb(71,123,121)` thành `Color.FromRgb(15,118,110)`.

## Cần xem bằng mắt
- Nút lịch của DatePicker (Tổng quan): style Button ngầm định có thể ảnh hưởng nút trong template của DatePicker.
- Tab Adjust/Retouch/Refine, accordion trong trình chỉnh màu, tab Curve/Levels nền tối.
- NumericStepper (giữ nút để lặp), danh sách phân đoạn và hàng chip ở Chế độ chụp; "Tốc độ phát" 0,25 / 0,5 / 1 / 2 trong chế độ 360°/Slow-motion.
- Thư viện khung: thumbnail, badge "Đang dùng", luồng Sửa và Xóa.
- Hàng thumbnail của Bố cục in: nhãn có còn bị cắt không.
- DataGrid: hàng chọn, ô đang sửa ở Bảng giá.
- Trang Bộ lọc màu còn nền trắng (do `WorkspaceToolsPanel` đặt `Background = Brushes.White`), các trang khác nền xám nhạt.

## Giữ nguyên, cần bạn quyết định
Các dòng chú thích thường trực còn lại từ bản gốc (AGENTS.md mục UI copy yêu cầu không có loại này), tôi không xóa vì bạn cấm xóa dòng:
- Cài đặt chung: "Loại tệp xuất · Cấu hình chờ kết nối xử lý ảnh".
- Bảng giá: "Giá trọn gói theo tổng số bản in · VNĐ".
- `LayoutWindow` (khối dự phòng ẩn sau các panel): các câu hướng dẫn của Thiết lập chụp và Cài đặt in.

## Chưa làm
Màn hình & thương hiệu, Căn chỉnh bản in, Quyền quản trị, Thông tin ứng dụng, Nhật ký hệ thống: chưa có ảnh chụp. Riêng "Màn hình & thương hiệu" đang hiện nội dung của Cài đặt chung; cần biết trang này phải chứa gì.
