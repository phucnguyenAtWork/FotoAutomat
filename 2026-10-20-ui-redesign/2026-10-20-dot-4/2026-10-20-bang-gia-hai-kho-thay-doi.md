# Đợt 4: Bảng giá hai khổ

Mỗi file là bản sao đầy đủ của file trong `src/`, chỉ khác ở các chỗ dưới đây. Chưa build.

## Hành vi
- Chỉ còn hai giá: Khổ nhỏ, Khổ lớn. Mặc định 90.000đ và 120.000đ (đoán từ "làm tròn con số", sửa được ở `BoothSessionPrices`).
- Admin chỉnh hai giá bằng NumericStepper, bước 5.000đ, 0 đến 1.000.000đ. Hai khổ có thể cùng 100.000đ.
- Khách chọn khổ ở màn chọn gói. Số bản in lấy từ Cài đặt in (`PrinterConfiguration.Copies`).
- Giá lưu ở file mới `session-prices.json` trong thư mục Operator. `prices.json`, `BoothPrice`, `Prices()`, `SavePrices` giữ nguyên, không còn được trang quản trị dùng.

## Thay đổi theo file
**BoothManagementStore.cs**
- Thêm trước `public sealed class BoothPrice`: `record BoothSessionPrices(int Small = 90000, int Large = 120000)`.
- Thêm sau `SavePrices`: `SessionPrices()` và `SaveSessionPrices(...)` (nguyên, 0 đến 100.000.000).

**CustomerPreviewFlow.cs**
- `PreviewPackage` thêm tham số cuối `string Name = ""` và `Label`.
- Constructor thêm tham số cuối `IReadOnlyList<PreviewPackage>? packages = null`. Không truyền thì dùng `Packages` cũ (test cũ không đổi).
- Thêm field `options`, thuộc tính `PackageOptions`.
- `Packages[PackageIndex]` đổi thành `options[PackageIndex]` ở `DeliverySummary`, `Amount`, `OrderSummary`.
- `SelectPackage`: `index is >= 0 and < 4` đổi thành `index >= 0 && index < options.Count`.
- `PackageHeading`: "Chọn gói in" đổi thành "Chọn khổ in".

**ManagementPanel.xaml**
- Thêm `xmlns:local`.
- Trang Bảng giá: bỏ dòng chú thích "Giá trọn gói theo tổng số bản in · VNĐ" và `PricesGrid` (DataGrid), thay bằng hai thẻ `SmallPrice`, `LargePrice`.
- Thanh dưới: thêm nút `RevertPricesButton` (Hoàn tác) và `DirtyText`.

**ManagementPanel.xaml.cs**
- Thêm `savedPrices`, `loadingPrices`, `LoadPrices`, `UpdatePriceState`, `OnPriceChanged`, `OnRevertPrices`.
- `ShowSection`: `PricesGrid.ItemsSource = store.Prices();` đổi thành `LoadPrices();`.
- `OnSavePrices` viết lại cho hai giá.

**MainWindow.xaml**: màn Layout còn hai RadioButton `q0`, `q1` (Content bind `PackageOptions[n].Label`); bỏ `q2`, `q3`.

**MainWindow.xaml.cs**: tạo flow kèm hai gói, giá từ `BoothManagementStore().SessionPrices()`, số bản từ `PrinterConfigurationStore().Load().Copies`.

**SessionPriceTests.cs** (file mới, đặt vào `tests/FotoAutomat.Tests/`): mặc định, lưu/đọc, 100k cả hai, từ chối ngoài khoảng, flow hai khổ.

## Chưa chắc
- Mặc định 90.000/120.000 là đoán.
- NumericStepper hiển thị theo `CurrentCulture`. Máy không phải vi-VN sẽ hiện dấu phẩy ngăn cách nghìn.
- Chưa có chỗ nào ghi doanh thu từ các giá này; phần doanh thu trong `sessions.json` không đổi.
- Chưa build, chưa chạy test; đã kiểm cú pháp C#, XML, khóa resource, handler.
