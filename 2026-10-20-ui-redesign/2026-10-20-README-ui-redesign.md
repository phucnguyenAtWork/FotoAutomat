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

## Đợt 3: trang mặc định, Căn chỉnh bản in, thanh cuộn Bố cục in, Bộ lọc màu gọn
Chưa biên dịch. Đã kiểm tra: XML hợp lệ, cú pháp C# (tree-sitter), khóa `StaticResource`, handler và `x:Name` của Căn chỉnh bản in còn đủ.

| File | Việc cần làm | Gốc bị thay |
|---|---|---|
| `2026-10-20-DefaultPagePanel.xaml` và `.xaml.cs` | Thêm, đổi tên thành `DefaultPagePanel.xaml`, `DefaultPagePanel.xaml.cs` | file mới |
| `2026-10-20-PrinterSettingsPanel.xaml` | Thay `PrinterSettingsPanel.xaml` | có |
| `2026-10-20-PrinterSettingsPanel.UiState.cs` | Thay `PrinterSettingsPanel.UiState.cs` | có (đợt 1) |
| `2026-10-20-LayoutWindow.xaml` | Thay `LayoutWindow.xaml` | có |
| `2026-10-20-LayoutWindow.FrameLibrary.cs` | Thay `LayoutWindow.FrameLibrary.cs` | có (đợt 2) |
| `2026-10-20-EditorUi.cs` | Thay `EditorUi.cs` | có |
| `2026-10-20-ColorEditorPanel.cs` | Thay `ColorEditorPanel.cs` | có |
| `2026-10-20-ColorAdvancedControls.cs` | Thay `ColorAdvancedControls.cs` | có |
| `2026-10-20-MatchLookTool.cs` | Thay `MatchLookTool.cs` | có |
| `2026-10-20-RetouchEditorPanel.cs` | Thay `RetouchEditorPanel.cs` | có |
| `2026-10-20-RefineEditorPanel.cs` | Thay `RefineEditorPanel.cs` | có |
| `2026-10-20-WorkspaceToolsPanel.cs` | Thay `WorkspaceToolsPanel.cs` | có |
| `2026-10-20-bo-loc-mau-thay-doi.md` | Danh sách từng chỗ đã sửa trong 7 file Bộ lọc màu, để áp dụng tay nếu không muốn thay nguyên file | tài liệu |

Bảy file `.cs` của Bộ lọc màu được tạo bằng cách sao đúng file đang có trong `src/` (bản bạn đã áp dụng ngày 20/10) rồi thay chuỗi; tôi không gõ lại cả file. Nếu bạn đã sửa các file này sau đó trong Claude Code thì dùng danh sách trong `2026-10-20-bo-loc-mau-thay-doi.md` thay vì thay nguyên file.

Việc cần làm trong `src/` (không nằm trong các file trên), tìm theo chuỗi:

`src/FotoAutomat.Desktop/ViewModels/LayoutEditorViewModel.cs`
- Tìm `public bool IsGeneralSettings => Section is "Cài đặt chung" or "Quyền quản trị" or "Màn hình & thương hiệu";`
- Thay bằng `public bool IsGeneralSettings => Section is "Cài đặt chung";`
- Bước này không bắt buộc: `DefaultPagePanel` nằm trên `GeneralSettingsPanel` nên đã che nội dung Cài đặt chung ở hai mục kia. Làm bước này để Cài đặt chung không tải dưới nền hai trang đó.

Màn hình & thương hiệu, Quyền quản trị:
- Hiện trang mặc định: tên mục và mô tả có sẵn của mục (`Section`, `SectionDescription`). Không nút, không ô nhập, không chữ mới. "Hiển thị default" tôi hiểu là trang mặc định như vậy; nếu ý bạn là tách thẻ của Cài đặt chung ra từng trang thì cần làm khác (đoán).
- Mã PIN vẫn nằm ở Cài đặt chung.

Máy in và Căn chỉnh bản in:
- Một panel, hai trang theo `Section`. Máy in: máy in, tùy chọn in, giới hạn in. Căn chỉnh bản in: chỉ thẻ căn chỉnh. Nếu không đọc được `Section` thì hiện tất cả như trước.
- Tỉ lệ, vị trí ngang, vị trí dọc dùng NumericStepper thay TextBox + Slider. Bước: tỉ lệ 0,5 %; vị trí 0,1 mm (đoán, chưa thử trên máy). Không gõ số trực tiếp được nữa; giữ nút để lặp. Nhãn bỏ đơn vị trong ngoặc vì đơn vị đã nằm trong ô.
- Tiêu đề trang vẫn là "Cài đặt in". Thanh lưu, Hoàn tác và chỉ báo chưa lưu dùng chung cho cả hai trang.

Bố cục in, cột thuộc tính bên phải:
- Một thanh cuộn duy nhất cho cả cột; danh sách LỚP nằm cuối cột, không còn thanh cuộn riêng và không còn cao cố định 180. Thứ tự giữ nguyên (thuộc tính trước, lớp sau), nên khi có ô được chọn thì phải cuộn xuống để thấy danh sách lớp. Có thể đổi để LỚP lên đầu cột nếu bạn muốn.
- Bỏ thẻ `ScrollViewer` bên trong của cột (thay bằng một `ScrollViewer` bọc ngoài). Con lăn trên danh sách lớp được chuyển lên vùng cuộn bằng `OnLayerListWheel`.

Bộ lọc màu (chỉ giao diện; logic bánh xe màu, đường cong, Levels, Match Look giữ nguyên, nhãn tiếng Anh giữ nguyên):
- Điều khiển thấp hơn: nút, ô chọn, hàng 44 → 36 và 40 → 32 (hằng số trong `EditorUi.cs`; muốn trả lại thì sửa hai số này).
- Thanh công cụ: Đổi ảnh, Vừa khung, Hoàn tác, Làm lại | Xem ảnh gốc, So với reference (vạch ngăn giữa hai cụm).
- Dải preset: thẻ 136 → 112 px, ảnh 78 → 60, viền 3 → 2. Dòng tên ảnh/trạng thái cỡ 12, màu trung tính, tối đa 2 dòng có dấu ba chấm (không tooltip). Nếu dấu ba chấm không hoạt động khi TextBlock xuống dòng thì chữ bị cắt ở dòng 2 (đoán).
- Cột phải là thẻ trắng có viền bo; histogram 116 → 96; dòng Clipping cỡ 11 màu cảnh báo của theme thay cho cam; "Lưu preset" là nút chính.
- Tab đường cong RGB/Luma/Red/Green/Blue: bỏ nền đen cứng, dùng màu teal như tab Levels.
- Match Look: ô reference 76 → 64; Xuất .cube và Áp dụng cùng khoảng cách.
- Retouch, Refine: cùng chia cột, cao co giãn (`MinHeight 560` thay `Height 650`), nút/ô chọn/hàng slider cùng kích thước với Adjust. Nhãn lệnh của hai tab này vẫn là "Fit", "Undo", "Redo" (Adjust dùng tiếng Việt); tôi không đổi vì bạn giữ nhãn tiếng Anh trong Bộ lọc màu.
- Nền trang theo `PageBrush` (dự phòng trắng); tab Adjust/Retouch/Refine 48 → 40.
- Không đổi: logic xử lý ảnh, `ColorReferenceComparison.cs`, `CompactColorControls.xaml`, Accordion dùng chung (cao tối thiểu 40, còn dùng cho các trang khác).

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
Cập nhật đợt 3: `WorkspaceToolsPanel.cs` (lề 24, tiêu đề 24 SemiBold) và `EditorUi.cs` (màu nhấn `#0F766E`) thấy đã áp dụng trong `src/`; hai mục đó dưới đây chỉ để tham khảo.

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
- Bộ lọc màu (đợt 3): cột phải dạng thẻ, dải preset, thanh công cụ, tab Retouch/Refine so với Adjust, tên ảnh dài có bị cắt đúng chỗ không, các trang Căn chỉnh bản in và hai trang mặc định, cột thuộc tính của Bố cục in (một thanh cuộn, con lăn trên danh sách lớp).

## Giữ nguyên, cần bạn quyết định
Các dòng chú thích thường trực còn lại từ bản gốc (AGENTS.md mục UI copy yêu cầu không có loại này), tôi không xóa vì bạn cấm xóa dòng:
- Cài đặt chung: "Loại tệp xuất · Cấu hình chờ kết nối xử lý ảnh".
- Bảng giá: "Giá trọn gói theo tổng số bản in · VNĐ".
- `LayoutWindow` (khối dự phòng ẩn sau các panel): các câu hướng dẫn của Thiết lập chụp và Cài đặt in.

## Chưa làm
- Thông tin ứng dụng, Nhật ký hệ thống: chưa có ảnh chụp, giữ nguyên giao diện hiện có của `WorkspaceToolsPanel`.
- Luồng giao diện khách: bạn nói chưa cần.
- Màn hình & thương hiệu, Quyền quản trị: mới là trang mặc định (tên + mô tả), chưa có nội dung riêng.
- Mục nhãn tiếng Việt cho lệnh trong Retouch/Refine: để nguyên.

## Đợt 4: Bảng giá hai khổ
Xem `2026-10-20-dot-4/2026-10-20-bang-gia-hai-kho-thay-doi.md`. Các file nằm trong thư mục `2026-10-20-dot-4`: BoothManagementStore.cs, CustomerPreviewFlow.cs, ManagementPanel.xaml, ManagementPanel.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs, SessionPriceTests.cs.
