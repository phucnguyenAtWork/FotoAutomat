# Xuất LUT `.cube` từ thông số — 14/10/2026

Luồng sản phẩm: **ảnh reference → Match Look trích xuất thông số (`ColorRecipe`, chỉnh tay được) → áp lên ảnh → nướng thông số đã tinh chỉnh thành `.cube`.** LUT được tạo ra **từ** thông số; không có AI sinh LUT trực tiếp.

## Cách nướng

`RecipeLutBaker.Bake(recipe, size)` (Core) cho từng màu trên lưới size³ đi qua chính `ColorProcessor` mà Adjust dùng để render, nên LUT và ảnh render dùng cùng một công thức. Red thay đổi nhanh nhất, khớp chuẩn `.cube` và `CubeLut`. `CubeLutFile.Save` (Infrastructure) ghi file `TITLE` / `LUT_3D_SIZE` / `DOMAIN_MIN` / `DOMAIN_MAX` với số theo `InvariantCulture`, nên máy cài tiếng Việt cũng không ghi dấu phẩy thập phân. Importer hiện có đọc lại được file này (có test round-trip).

## Phần không vào được LUT

LUT 3D chỉ biểu diễn được phép biến đổi **theo màu**. Các công cụ phụ thuộc vị trí pixel bị bỏ ra và được liệt kê:
- Clarity, Structure, Dehaze, Vignette, Grain, Sharpening;
- layer có mask Brush, Radial hoặc Linear.

Layer mask Full (ví dụ Match Look "Apply as new Layer") vẫn được nướng. UI báo "not in LUT: …" sau khi xuất.

## Độ chính xác (đo 14/10/2026)

So ảnh áp bằng LUT (`CubeLut.Evaluate`) với ảnh render trực tiếp bằng `ColorProcessor`, trên 40.000 màu ngẫu nhiên, với một look có Exposure, WB, Curves, HSL và Color Balance:

| Lưới | Lệch trung bình /255 | Lệch tối đa /255 |
|---|---:|---:|
| 33³ (mặc định) | 0,40 | 6 |
| 65³ | 0,27 | 3 |

Sai lệch đến từ: `ColorProcessor` nhận đầu vào 8-bit nên node lưới được làm tròn về byte gần nhất, và nội suy tam tuyến tính giữa các node. Look có HSL/hue thay đổi gắt có thể lệch nhiều hơn. Chưa thử trong Photoshop/Resolve và chưa kiểm chứng color management (file giả định sRGB đã mã hóa, domain 0–1).

## Dùng trong app

Adjust → Match Look → **Export .cube** xuất look đang chọn (33³). Chưa có tùy chọn 65³ trên UI; API hỗ trợ 2–65.
