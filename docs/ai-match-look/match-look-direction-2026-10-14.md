# Định hướng Match Look — 14/10/2026

Người dùng xác nhận lại mục tiêu: **Match Look trích xuất thông số chỉnh sửa** (recipe chỉnh tay được) từ một hoặc nhiều ảnh reference, giống Match Look của Capture One. Riêng biệt với đó, **thông số đã tinh chỉnh có thể nướng thành `.cube`**. AI chỉ phục vụ bước trích xuất thông số; LUT là phép tính xác định từ thông số.

```
Reference (1–8 ảnh) ──► [1] Trích xuất thông số ──► ColorRecipe (chỉnh tay được)
                                                        │
                                   [2] Áp lên ảnh ◄─────┤
                                                        │
                                   [3] Nướng .cube ◄────┘
```

Với đánh đổi về da, người dùng chọn phương án **A**: không ảnh nào da xấu đi.

## Look là bộ quy tắc; AI tách look khỏi nội dung (xác nhận 15/10)

Reference có thể là phong cảnh (nhà thờ, biển, núi) còn ảnh gốc là chân dung. Look là các quy tắc **không phụ thuộc nội dung**: theo độ sáng (tone, Color Balance), theo màu (HSL), toàn ảnh (ấm/lạnh, tương phản). Thông số tự áp theo độ sáng và màu, nên không cần biết "áp vào mặt hay vào đâu".

- **AI trên reference:** tách look khỏi nội dung bằng màu ký ức (vật trung tính, trời, mây, lá, da). Biển xanh là nội dung; mây ám cam là look.
- **AI trên ảnh gốc:** chỉ bảo vệ da (phương án A).

Hiện trạng:
- v4 chưa tách được: cảnh biển không chỉnh màu làm chân dung lệch xanh +92 (test đang Skip).
- v5a (điểm neo trung tính) tách được nhưng hỏng ca reference cùng loại nội dung: [v5a](v5a-content-separation-2026-10-15.md).
- Cần v5b: model phân đoạn cảnh, có kiểm tra giấy phép.

## Vì sao trước đó lệch hướng

- Ngày 05/10, CanonCGT (một model **sinh LUT trực tiếp**) được chọn làm nền cho AI Match Look. Bước [1] và [3] bị gộp: AI sinh LUT, thông số bị đẩy ra sau.
- Chuỗi thí nghiệm 12–13/10 tối ưu "LUT của CanonCGT khớp target bao nhiêu", mỗi bước sửa lỗi của bước trước. Không bước nào kiểm tra lại mục tiêu "ra thông số".

`AGENTS.md` vẫn ghi CanonCGT là nền phát triển Match Look; đã đề nghị người dùng quyết định sửa dòng đó.

## Đã làm 14/10 (triển khai và có test)

| Hạng mục | Mô tả | Tài liệu |
|---|---|---|
| Xuất `.cube` | `RecipeLutBaker` (Core) + `CubeLutFile` (Infrastructure) + nút Export .cube. 33³ lệch TB 0,40/255, tối đa 6 | [lut-export.md](../lut-export.md) |
| Color Balance 3 vùng | Nhóm Color Tone fit bánh xe Shadows/Mid/Highlights, trước Curves R/G/B | [reference-match.md](../reference-match.md) |
| Look-gram trong loss | Màu theo 8 dải độ sáng: phân biệt được tách tông và bản đảo ngược | như trên |
| Tách điểm phạt da / không phải da | Da (khóa màu YCbCr + chroma ≥ 30) bị phạt mạnh theo phương án A; vùng khác được nhuộm màu theo look. Reference rực rỡ: da +0,02 bão hòa (v2: +0,37) | như trên |
| Nhiều reference | 1–8 ảnh, trọng số bằng nhau; UI chọn/thả nhiều ảnh | như trên |

Kiểm chứng: 135 test, build toàn solution 0 cảnh báo, harness 4 kịch bản, MatchBenchmark 3/3. Tất cả là dữ liệu tổng hợp; **chưa kiểm chứng trên ảnh booth hay ảnh reference thật, và WPF chưa chạy trên màn hình** sau thay đổi.

## Giữ lại từ nghiên cứu CanonCGT

- **Hiểu biết về lỗi:** low-key sai do độ sáng toàn cục; drift tự tham chiếu chủ yếu là độ sáng; lỗi da không do vượt miền RGB; gamut mapping cần cho đầu ra hợp lệ; phương án A cho da.
- **Hạ tầng đánh giá:** bộ ảnh, mask da (MediaPipe), quy trình đăng ký tiêu chí trước, đối chứng cùng độ lớn, kiểm tra parity.
- **Dữ liệu:** pack 176 ảnh, look tổng hợp Phase B. Có thể tái dùng để tạo dữ liệu train cho bộ dự đoán thông số.
- **CanonCGT:** không còn là đường chính. Có thể làm đường so sánh hoặc nguồn dữ liệu phụ.

Ngày 14/10, theo yêu cầu người dùng, đã xóa 71 script nghiên cứu CanonCGT/LUT và `tools/LangSmithJudge`. Bản sao: `.local/archive/matchlook-canoncgt-research-2026-10-14.zip`. Các báo cáo trong thư mục này vẫn giữ làm lịch sử; lệnh tái hiện trong đó cần giải nén zip. Script còn lại: `tools/MatchLookData/README.md`.

## Bước tiếp theo đề xuất (chưa làm)

1. **Kiểm chứng v4 trên ảnh thật:** chạy Match Look với reference thật và ảnh booth, xem trên WPF.
2. **Bộ dự đoán thông số** (đã bàn với người dùng):
   - dấu vân tay look (look-gram, tone, điểm neo da / trung tính);
   - dữ liệu tổng hợp: ảnh gốc × recipe ngẫu nhiên render bằng `ColorProcessor`, nhãn chính xác;
   - mô hình nhỏ chạy CPU, cho thông số ban đầu tức thì;
   - optimizer hiện có tinh chỉnh tiếp.

   Đăng ký tiêu chí trước; đo trên nội dung chưa thấy.
3. **Tra thư viện look** (kiểu Shazam): so dấu vân tay reference với thư viện Digital Camera Looks.
4. **Kiểm chứng bảo vệ da trên ảnh thật:** chạy lại cặp `DSCF9358.JPG` / `test.jpg` (đã có số v2/v3 để so) và ảnh booth với nhiều tông da.
5. **Chụp bộ ảnh kiểm chuẩn tại booth:** holdout độc lập và ảnh nguồn chuẩn.
