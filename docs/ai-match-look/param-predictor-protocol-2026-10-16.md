# Protocol: AI có giám sát dự đoán thông số Match Look cho chân dung (16/10/2026)

Viết bởi Claude (Opus) theo yêu cầu người dùng. **Đây là protocol đề xuất, chưa train, chưa tải model hay dữ liệu nào.** Mọi mục đánh dấu ⚖ cần người dùng duyệt trước khi làm.

## 1. Mục tiêu

Học **ngược (reverse engineering)** từ pixel ra thông số: cho một ảnh reference (và ảnh cần chỉnh), mạng dự đoán `ColorRecipe` (Exposure, Temp/Tint, Contrast, Highlights/Shadows/Whites/Blacks, Saturation/Vibrance, Curves, HSL, Color Balance). Áp thông số lên ảnh chân dung thì **mặt và tổng thể giống reference**, không học nhầm nội dung (áo đen, tường hồng, biển) thành look. Kết quả vẫn chỉnh tay được và xuất `.cube` được, như Capture One.

Trọng tâm: **chân dung ở booth** (một hoặc nhiều người, tạo dáng, nền studio). Reference có thể là chân dung hoặc cảnh vật.

Vấn đề thật đang cần sửa:
- mặt bị **lóa sáng** khi reference sáng còn ảnh đích có áo tối;
- cảnh biển làm chân dung ám xanh (+92);
- v5a không học được bão hòa khi reference cùng loại nội dung.

## 2. Bằng chứng và mức được phép mượn

| Công trình | Cách họ làm (theo code và bài báo) | Giấy phép | FotoAutomat mượn gì |
|---|---|---|---|
| [Deep Preset (WACV 2021)](https://arxiv.org/pdf/2007.10701) · [code](https://github.com/minhmanho/deep_preset) | Reference đã chỉnh + ảnh màu tự nhiên → dự đoán **69 thông số Lightroom**. Dữ liệu: script Lightroom áp 500 preset lên 1.200 ảnh Flickr2K = 600.000 mẫu. PyTorch; output preset dạng JSON | **"Code and trained models are for non-commercial uses and research purposes only"** | **Chỉ ý tưởng**: cặp (ảnh, reference khác nội dung) → thông số; tạo dữ liệu bằng chính bộ render. Không dùng code/weights |
| [Exposure (Hu 2018)](https://arxiv.org/pdf/1709.09602) · [code](https://github.com/yuanming-hu/exposure) | Mạng xuất vector đặc trưng → 2 lớp FC → tách thành thông số từng filter; mỗi thông số qua `tanh_range(min, max)` để luôn nằm trong miền hợp lệ. Filter khả vi: exposure ×2^x, WB gain từng kênh (chuẩn hóa theo luma), curve từng đoạn tuyến tính N bước, contrast, saturation. Dự đoán trên ảnh nhỏ, áp lên ảnh lớn | **MIT** | **Ý tưởng và được tham khảo code**: đầu ra kiểu `tanh_range`, filter khả vi, dự đoán ở độ phân giải thấp (có ghi nguồn) |
| [RSFNet (ICCV 2023)](https://openaccess.thecvf.com/content/ICCV2023/html/Ouyang_RSFNet_A_White-Box_Image_Retouching_Approach_using_Region-Specific_Color_Filters_ICCV_2023_paper.html) · [code](https://github.com/Vicky0522/RSFNet) | Filter theo vùng: mặt nạ vùng (saliency, palette, ngữ nghĩa) + thông số filter riêng mỗi vùng; huấn luyện trên MIT-Adobe5K và **PPR10K (chân dung)** | **"For academic and non-commercial use only"** (Alibaba) | **Chỉ ý tưởng**: "AI biết vùng nào cần chỉnh". FotoAutomat dùng vùng để **ước lượng**, đầu ra vẫn là thông số toàn cục |
| [PPR10K (CVPR 2021)](https://github.com/csjliang/PPR10K) | 11.161 ảnh chân dung RAW, 3 chuyên gia chỉnh (XMP), mask vùng người. Loss **Human-Region Priority** (ưu tiên sai số vùng người) và **Group-Level Consistency** (cùng nhóm ảnh phải nhất quán) | Code **Apache-2.0**; **dữ liệu chỉ cho nghiên cứu phi thương mại** | **Ý tưởng và được tham khảo code loss** (HRP, GLC). **Không dùng dữ liệu** để train sản phẩm |
| [NCST (2024)](https://arxiv.org/pdf/2411.00335) | Đặc trưng VGG từ ảnh nội dung và ảnh style, fusion AdaIN → pooling → **7 thông số** (brightness, contrast, gamma, hue, saturation, sharpness, temperature); thông số → LUT để chạy nhanh; loss style + content + **histogram màu** | Không thấy code công bố | **Chỉ ý tưởng**: thông số → LUT (FotoAutomat đã có `RecipeLutBaker`), loss histogram làm phụ |

Capture One không công bố thuật toán. Tài liệu nói Match Look "uses existing Capture One tools to replicate the look … using AI" và cân nhiều reference theo "similarity" (ví dụ chân dung với phong cảnh). Đầu ra cùng dạng với các công trình trên, nhưng **không có bằng chứng** Capture One dùng đúng cách này.

## 3. Cách học ngược: dữ liệu tổng hợp có nhãn chính xác

Đây là cốt lõi, giống Deep Preset nhưng dùng bộ render của FotoAutomat thay cho Lightroom.

```
Ảnh nội dung A (chân dung booth, đa dạng)     Ảnh nội dung B (chân dung hoặc cảnh, KHÁC A)
          │                                                │
          │                       recipe ngẫu nhiên R      │
          ▼                                                ▼
   đầu vào mạng: A                          reference = ColorProcessor(B, R)
   đáp án ảnh: ColorProcessor(A, R)         đáp án thông số: R
```

- **A và B khác nội dung**: đây là cách mạng học "áo đen, tường hồng, biển là nội dung, không phải look". Cặp cùng nội dung chỉ chiếm một phần nhỏ.
- **R lấy theo phân bố thật**: preset người dùng đã lưu, preset XMP đã nhập, 8 look Phase B, cộng nhiễu ngẫu nhiên có giới hạn.
- **Render bằng tool .NET dùng chính `ColorProcessor`** (Core), nên nhãn khớp tuyệt đối với app. Không viết lại pipeline màu để tạo dữ liệu.
- **Thành phần nội dung** (khóa trước): ≥ 60% A là chân dung (1–4 người, áo sáng/tối, nền sáng/tối/màu); B gồm khoảng 50% chân dung và 50% cảnh (biển, trời, kiến trúc, cây). Có ca khó: áo đen + reference sáng, nhiều mặt, mặt nhỏ.
- **Chia theo nội dung và theo look**: test có **nội dung chưa thấy** và **look chưa thấy**; không trộn ảnh cùng buổi chụp giữa train và test.
- ⚖ Nguồn ảnh A/B: 176 ảnh pack hiện có (người dùng đã xác nhận quyền), Open Images đã chọn (kiểm quyền từng ảnh), ảnh booth chụp mới. **Không dùng dữ liệu PPR10K/MIT-Adobe5K** vì giấy phép phi thương mại.

## 4. Kiến trúc đề xuất (nhỏ, chạy CPU)

- **Hai nhánh mã hóa** (chia sẻ trọng số): reference → **vector look** (ý "vector phong cách" của CanonCGT/Deep Preset); ảnh đích → đặc trưng nội dung. Ảnh đầu vào 224–256 px (dự đoán ở ảnh nhỏ, áp lên ảnh lớn, như Exposure).
- **Kênh mặt/da**: thêm mask vùng người (MediaPipe đã có trong repo, ⚖ xác nhận giấy phép model) làm kênh đầu vào, để mạng biết mặt nằm đâu.
- **Ghép** hai vector → MLP → **khoảng 60 thông số** `ColorRecipe` toàn cục: Exposure, Warmth, Tint, Contrast, Highlights, Shadows, Whites, Blacks, Saturation, Vibrance, Curves 4 kênh × 3 điểm, HSL 8 × 3, Color Balance 3 × 2. Mỗi thông số qua `tanh_range` về đúng miền của `ColorRecipe.Validate()` (theo Exposure).
- ⚖ **Trọng số khởi đầu của backbone**: (a) **train từ đầu**: không vướng giấy phép, khả thi vì dữ liệu tổng hợp không giới hạn (khuyến nghị thử trước); (b) MobileNet/EfficientNet huấn luyện trên ImageNet: hội tụ nhanh hơn nhưng phải duyệt giấy phép weights và dữ liệu.

## 5. Hàm mất mát (loss)

1. **L1 trên thông số** (đã chuẩn hóa miền): ổn định lúc đầu, nhưng một ảnh có thể đạt được bằng nhiều bộ thông số, nên chỉ là phụ.
2. **Loss ảnh render**: render thông số dự đoán lên A, so với đáp án (ΔE trong Lab). Cần **bộ render khả vi** để tính gradient.
   ⚖ Đề xuất: bản PyTorch của `ColorProcessor` **chỉ dùng khi train**, có **test parity bắt buộc** với C# (lệch ≤ 1/255 trên tập màu chuẩn). C# vẫn là nguồn chuẩn cho app và cho đánh giá. Đây là ngoại lệ có kiểm soát với quy tắc "không viết pipeline màu song song" trong brief, nên cần người dùng duyệt.
3. **Ưu tiên vùng người (HRP, ý từ PPR10K)**: sai số vùng mặt/da nhân hệ số cao hơn (đặt trước, ví dụ 3×).
4. **Phương án A, chống mặt lóa**: phạt khi **độ sáng vùng mặt** lệch khỏi đáp án, khi da cháy sáng (clipping), và khi hue/saturation da lệch.
5. **Nhất quán look (ý GLC)**: cùng một R trên các B khác nhau phải cho thông số gần nhau, để không học theo nội dung reference.

## 6. Đánh giá (khóa trước khi chạy)

Trên test có nội dung và look chưa thấy, báo riêng từng nhóm (chân dung sáng/tối, nhiều mặt, reference chân dung/cảnh):
- **ΔE trung bình toàn ảnh** và **ΔE vùng mặt** so với đáp án render;
- **sai số độ sáng mặt** và **% pixel mặt bị cháy** (đo trực tiếp lỗi "mặt lóa");
- sai số thông số theo nhóm (Normalize / Light / Color / Tone);
- ca kiểm tra cố định: cảnh biển không chỉnh màu → chân dung không được ám màu; áo đen + reference sáng → mặt không lóa;
- so ba cách: **v4 hiện tại**, **mạng dự đoán**, **mạng dự đoán rồi v4 tinh chỉnh**;
- **thời gian trên CPU** (mục tiêu: dự đoán < 150 ms trên NUC; đo trên máy hiện tại trước, ghi rõ).

Tiêu chí tối thiểu để đưa vào app (đặt trước): "dự đoán + tinh chỉnh" phải tốt hơn v4 về ΔE vùng mặt **và** sai số độ sáng mặt, và không tệ hơn v4 ở ca cảnh biển, trên test chưa thấy. Sau đó mới thử trên ảnh thật của người dùng (cặp `DSCF9358`/`test.jpg` và cặp váy trắng / áo đen).

## 7. Tích hợp vào app

Export **ONNX** → chạy trong Infrastructure bằng ONNX Runtime (⚖ duyệt dependency). Core chỉ nhận `ColorRecipe` khởi đầu. Bước tối ưu v4 tinh chỉnh vài lượt. UI giữ nguyên: Apply → thông số → chỉnh tay → Lưu / Export .cube. Không có model nào sinh ảnh hay LUT trực tiếp.

## 8. Các giai đoạn

| Giai đoạn | Việc | Điều kiện qua |
|---|---|---|
| P0 | Tool .NET sinh dataset (A, B, R, render); ⚖ duyệt nguồn ảnh | Dataset tái lập được, có hash, chia train/test theo nội dung và look |
| P1 | Bản PyTorch khả vi của `ColorProcessor` + test parity với C# | Lệch ≤ 1/255 |
| P2 | Baseline: CNN nhỏ train từ đầu, loss thông số + render | Báo cáo theo mục 6, chưa đưa vào app |
| P3 | Thêm HRP, phương án A, nhất quán look; so với v4 | Đạt tiêu chí mục 6 |
| P4 | ONNX + tích hợp + thử ảnh thật và NUC | Người dùng duyệt bằng mắt |

Báo người dùng trước khi train. Không dùng 30 ảnh test dành cho nghiệm thu cuối trong P0–P3.

## 9. Cần người dùng quyết định (⚖)

1. Nguồn ảnh train (pack 176 ảnh, Open Images đã chọn, ảnh booth mới) và quyền dùng.
2. Backbone: train từ đầu hay dùng weights ImageNet (kèm duyệt giấy phép).
3. Cho phép bản PyTorch khả vi của `ColorProcessor`, chỉ dùng khi train, có test parity.
4. Máy để train: CPU hiện tại hay GPU (máy riêng / cloud). NUC chỉ dùng để chạy.
5. ONNX Runtime làm dependency của app.

## 10. Quyết định của người dùng (16/10/2026)

1. **Dữ liệu:** dùng những gì đang có: 974 ảnh Open Images (CC BY 2.0, phải giữ ghi nguồn tác giả trong manifest) và pack booth đã xác nhận quyền (trừ 30 ảnh test nghiệm thu và 20 ảnh cách ly). Ảnh booth chủ yếu giúp model phân biệt khung cảnh và vật thể (nội dung); thông số chỉ là một phần. Mục tiêu chính: engine **sao chép màu của reference lên ảnh preview** với thông số chuẩn.
2. **Backbone:** train từ đầu, không dùng weights có sẵn.
3. **Bản PyTorch khả vi của `ColorProcessor`:** đồng ý, chỉ dùng khi train, có test parity với C#.
4. **Train trên CPU**, để model đủ nhỏ cho mini PC.
5. **ONNX Runtime:** đồng ý.

## 11. Kết quả P0 (16/10/2026)

**Nhãn thông số**
- `RecipeVector` (Core): bố cục cố định **52 thông số**: Exposure, Warmth, Tint, 5 tone, Saturation, Vibrance, Curves RGB/R/G/B × 3 điểm, HSL 8 dải × 3, Color Balance 3 bánh × (x, y).
- Có dạng chuẩn hóa [-1, 1]. Decode luôn tạo recipe hợp lệ (curve đơn điệu). App sẽ dùng chính lớp này để đọc output ONNX.
- 3 test: layout cố định, encode/decode ổn định và render giống hệt, clamp và NaN.

**Recipe**
- Tool `tools/FotoAutomat.MatchDataset` (`schema`, `recipes`). Sinh **20.000 recipe** (seed 16):
  - 990 trung tính;
  - 6.880 biến thể quanh preset của người dùng;
  - 12.130 look ngẫu nhiên thưa (trung bình 22/52 thông số khác 0).
- Chia theo look: train 18.000 / validation 1.000 / test 1.000.
- **Hạn chế:** chỉ 3 preset làm điểm neo (Matched look, Matched look 1, Matched look 3). Preset LUT và trắng đen không mã hóa được, nên look neo còn ít đa dạng.

**Ảnh nội dung**
- `tools/MatchLookData/Prepare-ParamContent.py`: **1.116 ảnh** 256×256 BGRA (EXIF đã xoay, cắt giữa, không chỉnh màu).
  - 974 Open Images (CC BY 2.0, manifest giữ tác giả và link);
  - 142 ảnh booth (đã loại 30 test nghiệm thu, 20 cách ly và ảnh dự trữ test cũ).
- Phân loại: 842 chân dung, 274 cảnh.
- Chia theo nhóm (tác giả Open Images / buổi chụp booth):

  | | Chân dung | Cảnh |
  |---|---:|---:|
  | Train | 612 | 211 |
  | Validation | 154 | 28 |
  | Test | 76 | 35 |

- **Hạn chế:** booth chỉ có 3 nhóm. Buổi chụp session-01 nằm ở validation, không có ảnh booth trong test nội dung. Nghiệm thu booth cuối cùng vẫn dùng 30 ảnh dành riêng.
- Đã xem contact sheet: màu và hướng xoay đúng. Test: `Test-PrepareParamContent.py` (split, bố cục BGRA, EXIF).

**Output** (git-ignored): `.local/matchlook-data/param-dataset-2026-10-16/`: `param-schema.json`, `recipes.jsonl`, `content-set/` (manifest + `content/*.bgra`, 281 MB).

**Tiếp theo — P1:** bản PyTorch khả vi của `ColorProcessor` cho đúng 52 thông số + test parity với C# (cần thêm lệnh `render` vào tool để C# xuất ảnh chuẩn so sánh).

## 12. Kết quả P1 (16/10/2026): bộ render PyTorch khả vi

- `tools/MatchLookData/ColorRenderTorch.py`: chép đúng `ColorProcessor` cho recipe từ `RecipeVector.Decode`.
  - Bước 1: exposure/WB gain trong không gian tuyến tính, contrast, Highlights/Shadows/Whites/Blacks, Saturation/Vibrance → làm tròn byte.
  - Bước 2: Curves RGB rồi Curves kênh, HSL 8 dải, Color Balance → làm tròn byte.
  - Làm tròn half-to-even như C#. Chế độ `ste` (straight-through) để gradient đi qua bước làm tròn khi train.
- Tool .NET thêm lệnh `render`: C# xuất 200 ảnh chuẩn (cặp ngẫu nhiên giữa ảnh nội dung và recipe) vào `param-dataset-2026-10-16/parity-csharp/`.
- **Cổng parity (`Test-ColorRenderParity.py`): ĐẠT.** 200 cặp, khoảng 39 triệu giá trị màu, **lệch tối đa 0/255** (float64).
  - Mỗi nhóm thông số hoạt động trong 115–147/200 cặp (Normalize 142, Light 147, Color 125, Curves 128, HSL 126, Balance 115).
  - Ảnh đổi trung vị 11/255, tối đa 53/255, nên phép thử có ý nghĩa.
- Float32 (dùng khi train): lệch tối đa 2/255 ở 5/11,8 triệu giá trị. Chấp nhận cho train; parity chính thức vẫn đo bằng float64.
- Test gradient: mọi thông số nhận gradient hữu hạn; recipe trung tính giữ nguyên ảnh.
- Tốc độ CPU (máy hiện tại, 20 luồng), lô 16 ảnh, xuôi + ngược: 128 px float32 = 77 ms; 256 px float32 = 301 ms; 256 px float64 = 639 ms.

**Tiếp theo — P2:** baseline CNN nhỏ train từ đầu ở 128 px trên CPU. Loss = L1 thông số + loss ảnh render (ΔE, ưu tiên vùng người). Đánh giá theo mục 6 trên nội dung và look chưa thấy, so với v4. Báo người dùng trước khi chạy train.

## 13. P2 — đăng ký trước (16/10/2026, trước khi train)

**Mẫu train** (sinh ngay khi train, seed cố định):
- A = ảnh nội dung **train** (chân dung với xác suất 0,7, ngược lại cảnh);
- B = ảnh nội dung train **khác A** (chân dung 0,5 / cảnh 0,5);
- R = recipe **train**;
- đầu vào: ảnh đích A, reference = render(B, R); đáp án: thông số R và ảnh render(A, R).

Ảnh thu từ 256 về **128 px** (trung bình 2×2, làm tròn về byte).

**Model** (train từ đầu, khoảng 0,4 triệu tham số):
- một encoder CNN dùng chung: 5 tầng conv stride 2 (16-32-64-96-128 kênh, ReLU, không BatchNorm để export ONNX gọn), rồi gộp **trung bình và độ lệch chuẩn** toàn ảnh, ra 256 chiều;
- encoder áp cho cả reference và ảnh đích;
- head: [z_ref, z_đích, z_ref − z_đích] → MLP 768-256-52 → tanh → thông số [-1, 1].

**Loss:**
- L1 thông số (dạng [-1, 1]);
- + **ΔE76 trong Lab** giữa render(A, dự đoán) và render(A, R), chia 10;
- trọng số ảnh: pixel màu da (khóa YCbCr của engine, chroma ≥ 30) ×3 (ưu tiên vùng người, ý từ PPR10K). Mask MediaPipe để dành cho P3;
- tổng = L_thông_số + L_ảnh.

**Tối ưu:**
- Adam, lr 1e-3, cosine giảm về 1e-5; lô 16; 12 luồng CPU (chừa CPU cho người dùng);
- số bước chốt từ đo thời gian 50 bước, với **ngân sách ≤ 2 giờ** và tối đa 15.000 bước;
- checkpoint tốt nhất chọn theo loss **validation** (nội dung validation × recipe validation, 200 mẫu cố định). Không nhìn test khi chọn.

**Đánh giá trên test** (nội dung test × recipe test, **300 mẫu cố định**, seed 2026, chỉ chạy một lần sau khi train xong), bằng render float64 (khớp C# 0/255):
- ΔE76 trung bình toàn ảnh;
- ΔE trên vùng da (khóa màu);
- **sai số L\* trung bình vùng da** (chỉ báo mặt lóa/tối);
- % pixel da cháy (L\* > 97) thừa so với đáp án;
- MAE thông số theo nhóm.

Thêm **50 mẫu "reference cảnh không chỉnh màu"** (B = cảnh, R = trung tính): ΔE giữa kết quả và A (đáp án là giữ nguyên ảnh).

**So với v4:** chạy `ReferenceMatchEngine.Fit` (C#, mặc định, **không có vùng mặt** vì tool không chạy được nhận diện mặt Windows) trên đúng các cặp test, render bằng C#, đo cùng thước đo.

**Tiêu chí P2 (cổng nghiên cứu, chưa đưa vào app):** model tốt hơn v4 cả ba: ΔE toàn ảnh, ΔE vùng da và sai số L\* vùng da trên 300 mẫu test; và ở 50 mẫu cảnh không chỉnh màu, model không tệ hơn v4. Báo đủ mọi số dù đạt hay không. Không đổi tiêu chí sau khi xem kết quả.

**Giới hạn biết trước:**
- ảnh 128 px;
- ảnh Open Images vốn đã được chỉnh màu (look học là phép biến đổi tương đối);
- "da" theo khóa màu, không phải mask ngữ nghĩa;
- v4 trong phép so sánh không có ưu tiên vùng mặt.

## 14. Kết quả P2 (16/10/2026)

**Train:**
- 15.000 bước, 37 phút CPU (12 luồng), model 399.924 tham số;
- checkpoint tốt nhất ở **bước 3.000** (val 1,038). Sau đó loss validation **tăng** (1,185 ở bước 12.000): model học thuộc dữ liệu train mà không tổng quát hóa;
- sai số thông số validation 0,143–0,148, **không tốt hơn mốc luôn đoán 0** (0,138).

**Đánh giá một lần trên test cố định** (`.local/matchlook-data/param-predictor-p2-eval/`):

| | Model | v4 | Không chỉnh gì* |
|---|---:|---:|---:|
| ΔE toàn ảnh (300) | 8,57 | 17,56 | 9,19 |
| ΔE vùng da | 9,69 | 17,19 | 10,39 |
| Sai số L\* vùng da | 4,48 | 9,58 | — |
| ΔE, 50 mẫu reference cảnh không chỉnh màu | 3,18 | 17,46 | 0 |

\* Mốc "không chỉnh gì" **không có trong đăng ký trước**; được đo **sau khi xem kết quả**, để diễn giải.

**Theo tiêu chí đã đăng ký: ĐẠT** (cả 4 mục). **Nhưng kết quả này rỗng:**
- model gần như **không chỉnh gì**: độ lớn thông số dự đoán trung bình 0,02 so với thực tế 0,134;
- nó chỉ tốt hơn "không chỉnh gì" khoảng 7% (ΔE);
- nó thắng v4 vì v4 **tệ gấp đôi "không chỉnh gì"** trên tập này. Đây là lỗi v4 bắt chước màu nội dung reference khi reference khác cảnh.

**Lỗ hổng thiết kế:** tiêu chí P2 thiếu mốc "không chỉnh gì". Từ P3 bắt buộc có mốc này, và model phải vượt nó một khoảng đăng ký trước.

**Chẩn đoán** (ngoài đăng ký, ghi rõ): hồi quy tuyến tính trên thống kê màu (trung bình, độ lệch, phân vị, Lab) của reference và ảnh đích chỉ giải thích được Exposure R² 0,29, các thông số khác ≤ 0,13, **trung vị R² 0,04**. Bài toán gần như **không xác định được** với dữ liệu hiện tại:
- reference = ảnh B đã chỉnh, mà B là ảnh bất kỳ, vốn đã có màu riêng;
- recipe ngẫu nhiên thưa và nhẹ (ảnh đổi trung vị 11/255), nhỏ hơn khác biệt màu tự nhiên giữa các ảnh;
- khi không phân biệt được, đoán "gần 0" là lựa chọn tối ưu của model.

Đây cũng chính là gốc của lỗi v4 (nhầm nội dung thành look), giờ hiện ra ở dạng dữ liệu.

**Hướng P3 (đề xuất, chưa làm):**
1. **Nhiều reference cùng look** (3–5 ảnh khác nội dung, như Capture One khuyến nghị): nội dung triệt tiêu nhau, look còn lại.
2. **Look mạnh và nhất quán như look thật** (preset người dùng sẽ tạo, preset XMP), thay vì nhiễu ngẫu nhiên thưa.
3. Thêm mốc "không chỉnh gì" và "v4" vào mọi tiêu chí.
4. Kiểm tra lại giới hạn trên trước khi train: đo R² tuyến tính với nhiều reference; nếu vẫn thấp thì đổi thiết kế, chưa train tiếp.

## 15. Kiểm tra khả năng nhận ra look với nhiều reference — đăng ký trước (16/10/2026)

**Phương pháp** (không train mạng):
- hồi quy ridge (λ = 10) trên thống kê màu: trung bình, độ lệch, phân vị 5/25/50/75/95% RGB, trung bình và độ lệch Lab;
- **đặc trưng reference = trung bình thống kê của K reference** (cùng recipe R, các ảnh B khác nhau, không trùng A), cộng thống kê ảnh đích A và hiệu hai bên;
- K ∈ {1, 3, 5}; train 6.000 mẫu, validation 1.000 mẫu (nội dung và recipe validation);
- đo R² từng thông số. Ridge tuyến tính là **cận dưới**: mạng nơ-ron có thể khá hơn, nhưng nếu ridge thấp quá thì là dấu hiệu xấu.

**Ngưỡng quyết định** (đặt trước): với K = 5, **trung vị R² ≥ 0,30 và R² của Exposure, Warmth, Tint đều ≥ 0,50** → đi tiếp P3 theo hướng nhiều reference. Không đạt → đổi thiết kế dữ liệu/look trước khi train.

### Kết quả mục 15 — KHÔNG ĐẠT ngưỡng, đổi thiết kế trước khi train

| K reference | Trung vị R² (52 thông số) | Exposure | Warmth | Tint |
|---:|---:|---:|---:|---:|
| 1 | 0,03 | 0,14 | 0,07 | 0,04 |
| 3 | 0,06 | 0,37 | 0,15 | 0,12 |
| 5 | 0,07 | 0,52 | 0,19 | 0,13 |

Nhiều reference có giúp (Exposure 0,14 → 0,52), nhưng xa ngưỡng (trung vị ≥ 0,30; Warmth và Tint ≥ 0,50).

**Thăm dò sau đó** (ngoài đăng ký, ghi rõ):
1. **Thông số chồng chéo.** Đoán "chữ ký look" (kết quả look trên bảng 64 màu, đơn vị Lab) dễ hơn đoán từng thông số: R² 0,09 / 0,25 / **0,32** với K = 1 / 3 / 5, so với 0,03 / 0,06 / 0,07. Nhiều tổ hợp thông số (Warmth, Curves R/B, Color Balance, HSL) cho ra cùng một look, nên giám sát từng thông số là đặt bài toán sai.
2. **Độ mạnh của look** (K = 5, chữ ký):
   - look nhẹ (ΔE 6,5): sai số dự đoán 8,7, lớn hơn bản thân look;
   - look vừa (11,9): sai số 10,4;
   - look mạnh (19,6): sai số 12,5, tỷ lệ 0,64.
   Recipe ngẫu nhiên hiện tại phần lớn quá nhẹ so với khác biệt màu nội dung.
3. Sai số tuyệt đối khoảng 10–12 ΔE vẫn còn với thống kê tuyến tính: nhập nhằng nội dung/look là giới hạn chính. Đây là chỗ mạng nơ-ron **có thể** vượt thống kê tuyến tính nhờ nhận ra vật có màu ký ức (da, trời, vật trung tính), nhưng chưa được chứng minh.

**Thiết kế P3 đề xuất:**
- (a) đích học = **chữ ký look** (hoặc LUT thưa), rồi suy ra thông số bằng fit xác định (PairedMatchEngine trên bảng màu);
- (b) **thư viện look thật**, mạnh và nhất quán (người dùng duyệt);
- (c) **K = 3–5 reference** khi train và trên UI;
- (d) trước khi train dài: so CNN (học trên chữ ký) với ridge trên cùng split. CNN phải vượt ridge thì mới đi tiếp.

## 16. Thiết kế P3 + guardrail — đăng ký trước (17/10/2026)

Người dùng xác nhận: model phải học **đủ 52 thông số** hiển thị trong WPF, có **guardrail để thông số không vượt ngưỡng** và model **biết cân bằng thông số**. Tham khảo kiến trúc Deep Preset (Fig. 3, WACV 2021; chỉ mượn ý tưởng, code và weights của họ là phi thương mại).

**Kiến trúc** (đều train từ đầu, CPU):
- **Encoder T** nhận **reference ghép với ảnh đích** theo kênh (6 kênh) từ đầu, để so trực tiếp vùng màu của hai ảnh (ý Encoder T của Deep Preset). Nhiều reference: chạy T cho từng reference rồi lấy trung bình vector.
- **FC** (Linear + LeakyReLU … Linear + **Tanh**) → 52 thông số.
- **Không có Decoder G.** Ảnh kết quả = 52 thông số đi qua bộ render khả vi (khớp C# 0/255), nên thông số trên thanh trượt chính là thứ tạo ra ảnh.

**Guardrail lớp 1 — chặn cứng:** Tanh × khoảng an toàn của từng thông số (`RecipeVector`). Model không thể xuất giá trị ngoài khoảng. Khoảng an toàn là cấu hình, có thể thu hẹp khi chạy mà không train lại.

**Guardrail lớp 2 — cân bằng** (loss khi train):
- **tiết kiệm:** phạt tổng |thông số| có trọng số theo nhóm: Normalize 1, Light & Contrast 1, Color 1,5, Curves 2, HSL 2, Balance 2. Nếu hai cách cho cùng ảnh, model chọn cách dùng công cụ đơn giản (Warmth thay vì Curves R/B);
- **không đá nhau:** phạt khi Warmth và trục ấm/lạnh của Color Balance ngược dấu nhau, và khi Exposure ngược dấu với tổng Whites/Highlights;
- **Lpp** (ý Deep Preset): cùng một look áp lên các reference khác nhau phải cho vector look (lớp FC giữa) gần nhau.

**Guardrail lớp 3 — an toàn ảnh** (loss khi train, kiểm tra lại khi chạy):
- **da (phương án A):** phạt lệch hue/saturation da và lệch độ sáng da so với đáp án; phạt tăng pixel da cháy (L\* > 97);
- **không tạo vùng cháy/đen kịt mới** so với đáp án;
- **khi chạy trong app:** nếu kết quả vi phạm ngưỡng da/cháy thì **tự giảm Impact** tới khi đạt, và báo người dùng.

**Dữ liệu:**
- look lấy từ **thư viện look chân dung do người dùng duyệt** (bước 2), cộng biến thể (độ mạnh 0,6–1,2, nhiễu nhỏ);
- **K = 1–5 reference** khác nội dung cho mỗi look.

**Tiêu chí** (sẽ khóa trước lượt train, sau khi thư viện look được duyệt):
- so với **không chỉnh gì**, **v4**, và **ridge trên thống kê màu**, trên test chưa thấy;
- model phải vượt cả ba về ΔE toàn ảnh và ΔE vùng da, **và** không vi phạm guardrail lớp 3 ở quá 2% mẫu.

### Bước 2: thư viện look v1 chờ người dùng duyệt (17/10/2026)

- `tools/MatchLookData/look-library-v1.json`: **30 look chân dung**, 13 họ (natural, korean, film, cinematic, vintage, moody, bright, cool, warm, vivid, pastel), viết bằng tên thông số của app. Mọi giá trị đã kiểm tra nằm trong guardrail lớp 1 (script từ chối giá trị ngoài khoảng).
- `tools/MatchLookData/Render-LookLibrary.py` → `.local/matchlook-data/look-library-review-2026-10-17/index.html`: mỗi look áp lên 4 ảnh booth và 1 ảnh Open Images (render khớp app), kèm chỉ số da trung bình (phương án A).
- **Look bị cảnh báo da** (ngưỡng tạm: |ΔL\*| > 8, hue da > 8°, da cháy mới > 1%):
  - lệch hue da: pink-pastel 12,2°, vintage-faded 10,1°, sepia-soft 14,5°, cool-blue 9,3°, mint-fresh 11,7°, cine-green 10,1°, lavender 20,0°, retro-70s 15,3°, cross-process 12,7°, night-neon 9,9°;
  - độ sáng da: moody-dark −9,6, low-key-warm −9,6;
  - da cháy: noir-cool 2,4%.
- Look nhuộm màu (tint) gần như luôn làm lệch hue da: đó là bản chất của look. Người dùng cần quyết định: giữ look tint (và nới ngưỡng hue da cho nhóm này), hay loại bỏ. Quyết định này ảnh hưởng trực tiếp guardrail lớp 3.
- Ảnh booth trong bảng là ảnh khách: chỉ xem cục bộ, không đưa lên mạng.

## 17. P3 — chi tiết đăng ký trước (17/10/2026, trước khi train)

**Người dùng (17/10):** duyệt cả 30 look (sẽ theo dõi và xóa sau look không cần); P3 phải xin phép trước khi train. Look tint được giữ. Vì vậy guardrail da khi train so với **ảnh đáp án đã áp look**, không so với ảnh gốc: look được phép đổi màu da đúng như look, model không được làm da lệch thêm.

**Chia look** (`tools/MatchLookData/look-splits-p3.json`):
- **test**: peach-warm, portra-like, cine-green, retro-70s, night-neon (mỗi họ chính một look; không bao giờ xuất hiện khi train);
- **validation**: soft-glow, mint-fresh, sunny-summer;
- **train**: 22 look còn lại.

Ảnh nội dung giữ split cũ (theo tác giả / buổi chụp).

**Mẫu train:**
- 80% look train × độ mạnh U(0,6; 1,2) × nhiễu N(0; 0,05) trên các thông số look dùng (dạng [-1, 1], kẹp lại);
- 20% recipe ngẫu nhiên từ split train cũ (phủ không gian);
- K reference ~ đều {1, …, 5}, mỗi reference là một ảnh B khác nhau và khác A;
- A chân dung 0,7.

**Model** (`ParamPredictorP3.py`, train từ đầu):
- Encoder T: 6 kênh [reference, đích] → 6 conv stride 2 (16-32-64-96-128-160) → trung bình + độ lệch toàn ảnh (320);
- K reference: trung bình vector;
- FC 320 → 256 (LeakyReLU) = **vector look**;
- FC 256 → 256 (LeakyReLU) → 52 → Tanh.

**Loss** (hệ số khóa trước):
- ΔE76 Lab (da ×3) / 10;
- + 0,2 · L1 thông số;
- + 0,02 · tiết kiệm Σ w_g·|u| (w: Normalize 1, Light 1, Color 1,5, Curves 2, HSL 2, Balance 2);
- + 0,05 · đá nhau (Warmth ngược dấu trục ấm của Color Balance; Exposure ngược dấu **Whites**). *Sửa trước khi train (17/10): bản đăng ký đầu phạt cả Exposure ngược dấu Highlights, nhưng "tăng Exposure, kéo Highlights xuống" là kỹ thuật hợp lệ dùng trong các look đã duyệt (high-key, korean-airy, clean-bright). Đã kiểm tra: phần Warmth không phạt look nào trong 30 look.*
- + 0,1 · Lpp (khoảng cách vector look giữa hai bộ reference khác nhau của **cùng** recipe);
- + 0,5 · |ΔL\*| vùng da so với đáp án / 10;
- + 1 · phần pixel da cháy (L\* > 97) vượt đáp án.

**Đánh giá test** (một lần, seed 2027): 300 mẫu (nội dung test × 5 look test, độ mạnh U(0,6; 1,2)), K = 3 (chính) và K = 1 (báo cáo). Đo ở 256 px, render float64.

**Mốc so sánh:**
- (1) không chỉnh gì;
- (2) v4 `FitReferences` với cùng K reference (không vùng mặt);
- (3) ridge trên thống kê màu dự đoán 52 thông số (train trên 6.000 mẫu train cùng phân bố).

**Tiêu chí P3:**
- với K = 3, model tốt hơn **cả ba mốc** về ΔE toàn ảnh **và** ΔE vùng da;
- **vi phạm guardrail lớp 3 ≤ 2% mẫu** (vi phạm = |ΔL\*| da so với đáp án > 8, hoặc da cháy mới vượt đáp án > 1%);
- model tốt hơn "không chỉnh gì" ít nhất **15%** ΔE (tránh kết quả rỗng như P2).

**Kiểm tra trước khi train** (cũng một lần): ridge R² trên look thư viện, K = 1/3/5, cho thông số và chữ ký look. Chỉ báo cáo, không phải cổng; người dùng duyệt trước khi train.

### Kết quả kiểm tra trước khi train (17/10/2026)

Script `tools/MatchLookData/Check-P3Feasibility.py` (lưu trong repo, khác mục 15), kết quả trong `.local/matchlook-data/p3-feasibility-2026-10-17/`.
- Mẫu chỉ dùng look thư viện (khi train còn thêm 20% recipe ngẫu nhiên).
- Ridge λ = 10.
- "Chữ ký look" = 64 màu lưới RGB qua recipe, đo ΔE Lab.

**A. Nội dung mới, look đã biết** (validation content × 22 look train):

| K | Trung vị R² thông số | Exposure | Warmth | Tint | ΔE chữ ký: ridge | không chỉnh | trung bình train |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 0,06 | 0,19 | 0,09 | 0,06 | 9,1 | 11,3 | 10,3 |
| 3 | 0,09 | 0,35 | 0,17 | 0,15 | 7,9 | 11,3 | 10,3 |
| 5 | 0,10 | 0,42 | 0,25 | 0,22 | **7,4** | 11,3 | 10,3 |

**B. Look chưa thấy** (3 look validation):

| K | ΔE chữ ký: ridge | không chỉnh | trung bình train |
|---:|---:|---:|---:|
| 1 | 8,0 | 7,4 | 8,2 |
| 3 | 7,6 | 7,4 | 8,2 |
| 5 | 7,3 | 7,4 | 8,2 |

R² ở B âm (−0,46 … −0,49): ba look này nhẹ và chỉ 14 thông số có biến thiên, nên R² không có ý nghĩa ở đây; đọc theo ΔE.

**C. Ngoài đăng ký, ghi rõ: bỏ-một-look** (25 look không thuộc test; mỗi lần train ridge trên 24 look, thử trên look còn lại, nội dung validation; không chạm look test):

| K | ΔE chữ ký: ridge | không chỉnh | trung bình train | Số look ridge thắng "không chỉnh" |
|---:|---:|---:|---:|---:|
| 1 | 9,6 | 10,8 | 10,4 | 12/25 |
| 3 | 8,7 | 10,8 | 10,4 | 12/25 |
| 5 | **8,1** | 10,8 | 10,4 | 12/25 |

**Đọc kết quả:**
- Với look đã biết, nhiều reference giúp rõ (ΔE 11,3 → 7,4 ở K = 5, −34%).
- Với look chưa thấy, ridge thắng trung bình (−24% ở K = 5 theo C) nhưng **chỉ thắng ở 12/25 look**:
  - thắng mạnh ở look mạnh: sepia-soft 31,9 → 16,9; vintage-faded 23,2 → 9,9; film-matte 15,5 → 5,0; muted-earth 18,7 → 7,8;
  - **thua ở look nhẹ**: rosy-skin 1,2 → 7,6; warm-studio 2,9 → 7,2; cool-studio 3,8 → 7,0 — ridge luôn "thêm look" khoảng 7 ΔE dù look gốc gần như không đổi gì.
- **Rủi ro cho tiêu chí P3:** 22 look train là ít. Model mạng có thể chủ yếu học nhận ra look thư viện, và với look test nhẹ (peach-warm, portra-like) có thể không vượt "không chỉnh" 15%. Hệ số tiết kiệm (economy) có thể giúp ở look nhẹ, nhưng chưa có bằng chứng.

**Thời gian** (đo forward + backward trên batch 16, 12 luồng CPU, không bước tối ưu, không lưu model): 0,23 / 0,37 / 0,48 giây/bước với K = 1 / 3 / 5, trung bình ≈ 0,37 giây/bước → 30.000 bước ≈ **3,1 giờ** (+ validation mỗi 500 bước). Model 536 nghìn tham số. Ở bước đầu mọi loss hữu hạn.

**Sửa trước khi train (17/10):** `Evaluate-ParamPredictorP3.py` render reference từ ảnh 128 px như khi train (bản đầu render 256 px rồi thu nhỏ; render không giao hoán với thu nhỏ).

### Phương án (b): tăng cường look — đăng ký trước khi train (17/10/2026)

**Người dùng (17/10):** chọn (b). Lý do: kiểm tra trên cho thấy 22 look train dễ khiến model chỉ nhận ra look thư viện, và ridge thua "không chỉnh" ở các look nhẹ.

**Thay đổi duy nhất so với phần đăng ký ở trên** — cách tạo look cho 80% mẫu train (`ParamPredictorP3.look_augment`):
- 1 look (50%), trộn 2 look (30%) hoặc 3 look (20%) trong 22 look train; trọng số Dirichlet(1);
- bỏ từng **nhóm điều khiển** đang dùng với xác suất 0,2 (một thanh trượt / một trường HSL / 3 điểm của một curve / X-Y của một bánh xe), luôn giữ ít nhất một nhóm;
- độ mạnh U(0,3; 1,3) (trước: U(0,6; 1,2)) để có cả look rất nhẹ;
- nhiễu N(0; 0,05) trên thông số đang dùng, kẹp [-1, 1].

**Giữ nguyên:**
- 20% recipe ngẫu nhiên;
- model, loss, hệ số;
- validation (look validation đơn, U(0,6; 1,2), K = 3);
- tập test 300 mẫu, các mốc so sánh và tiêu chí.

Mốc ridge (`--train-ridge`) dùng cùng phân bố train đã tăng cường.

**Kiểm tra ridge bỏ-một-look** (`Check-P3Feasibility.py --lolo --augment`; mẫu đánh giá không tăng cường; mẫu train có chứa look bị bỏ ra đều bị loại):

| K | ΔE chữ ký: không tăng cường | tăng cường | không chỉnh | Số look thắng "không chỉnh": không / có tăng cường |
|---:|---:|---:|---:|---:|
| 1 | 9,59 | 9,27 | 10,76 | 12 / 14 |
| 3 | 8,74 | 8,49 | 10,76 | 12 / 13 |
| 5 | 8,13 | **7,98** | 10,76 | 12 / 14 |

Chạy lại không tăng cường cho đúng số cũ (mã đổi không làm lệch).

Tăng cường giúp nhẹ ở mức tuyến tính:
- look nhẹ khá hơn (rosy-skin 7,6 → 6,2; warm-studio 7,2 → 5,8; soft-glow 5,8 → 4,8);
- look rất mạnh kém đi (sepia-soft 16,9 → 20,5; noir-cool 12,3 → 15,0), do phân bố độ mạnh dịch xuống.

Cổng đặt ra: "không tệ hơn khi không tăng cường" → **đạt**, train.

**Lần train:** `Train-ParamPredictorP3.py --budget-hours 3.5` (tối đa 30.000 bước), 12 luồng CPU, seed 23. Kết quả vào `.local/matchlook-data/param-predictor-p3-001/`.

## 18. Kết quả P3 (17/10/2026) — KHÔNG ĐẠT

**Train:**
- 30.000 bước, 143 phút, CPU 12 luồng.
- `best.pt` ở bước 3.500 (val total 0,772); sau đó validation đi ngang (0,79–0,80).
- Thư mục: `.local/matchlook-data/param-predictor-p3-001/`.
- SHA-256 thư viện look và file chia look được ghi trong `done.json`.

**Test** (300 mẫu, 5 look test, render float64; `.local/matchlook-data/param-predictor-p3-eval/`):

| Phương pháp | ΔE toàn ảnh | ΔE da | \|ΔL\*\| da | Vi phạm guardrail |
|---|---:|---:|---:|---:|
| Không chỉnh gì | 5,20 | 5,53 | 2,07 | 0,7% |
| **Model P3, K = 3** | 5,16 | 5,57 | 2,05 | 2,3% |
| Model P3, K = 1 | 5,16 | 5,57 | 2,05 | 2,3% |
| Ridge, K = 3 | **4,64** | **5,22** | 1,97 | 2,0% |
| Ridge, K = 1 | 6,19 | 6,84 | 2,81 | 6,3% |
| v4 `FitReferences`, K = 3 | 12,18 | 11,37 | 5,51 | 18,3% |
| v4, K = 1 | 16,18 | 14,35 | 8,19 | 31,3% |

**Tiêu chí:**
- tốt hơn "không chỉnh gì": không (ΔE da kém hơn);
- tốt hơn v4: có;
- tốt hơn ridge: không;
- vi phạm ≤ 2%: không (2,3%);
- tốt hơn "không chỉnh gì" ≥ 15%: không (0,9%).

→ **Không đạt.**

**Chẩn đoán** (sau test, ghi rõ):
- Model **sụp về một đầu ra hằng số**: |u| trung bình 0,007, tức gần như không chỉnh gì.
- Đổi reference làm đầu ra đổi chưa tới 1e-4; đổi ảnh đích không làm đổi gì. Vì vậy K = 1 và K = 3 cho cùng kết quả.
- Model sụp từ sớm (trước bước 3.500).
- Ridge K = 3 tốt hơn "không chỉnh gì" 11% trên cùng tập test. Vậy **tín hiệu có trong dữ liệu**; thất bại nằm ở tối ưu hoặc thiết kế loss, không phải ở dữ liệu.
- Nghi vấn chưa kiểm chứng:
  1. Lpp chuẩn hóa theo ‖z‖² có nghiệm tầm thường là z hằng số;
  2. economy kéo u về 0;
  3. gradient của ảnh qua renderer STE yếu so với hai số hạng trên;
  4. pooling mean/std trên ảnh ghép 6 kênh làm mất tương quan reference–đích.

**Phát hiện phụ:** v4 với reference **khác nội dung** tệ hơn "không chỉnh gì" 2,3 lần, vi phạm da 18%. Điều này khớp giới hạn đã biết của v4 (nội dung bị hiểu nhầm là look) và là lý do nên dùng reference cùng loại ảnh.

**Không chỉnh tiếp khi chưa có người dùng duyệt** (bài học từ v5a).

**Đề xuất P3b** (đăng ký trước nếu được duyệt):
- ablation ngắn khoảng 20 phút mỗi lần, đo độ nhạy đầu ra theo reference;
- (a) bỏ Lpp và economy;
- (b) giám sát thông số trước (khởi động bằng L1 thông số), rồi mới thêm loss ảnh;
- (c) ghép đặc trưng reference và đích sau encoder thay vì ghép kênh.
- Cổng: model phải nhạy theo reference và tốt hơn ridge trên validation, rồi mới train dài.
