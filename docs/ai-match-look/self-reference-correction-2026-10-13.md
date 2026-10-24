# Self-reference correction và LUT đóng băng — 13/10/2026

Thực hiện bởi Claude (Opus) theo yêu cầu người dùng, tiếp nối `self-reference-2026-10-13.md` của Codex. Chỉ suy luận, không optimizer, không dùng test, không sửa script/checkpoint của Codex.

## Tiền đăng ký (ghi trước khi chạy)

### Cơ sở

Canonicalizer và Restyler đều là 3D LUT 17³ toàn cục theo từng ảnh (`LookUpTable_Estimator`, `TrilinearInterpolation`). Gọi `M_ref(x)=L_r(L_c(x))` và `M_self(x)` là chuỗi tương ứng khi reference = source. Correction cộng

`D(x) = M_ref(x) − α·cap(M_self(x) − x)`

là một hàm màu toàn cục, nên correction trên pixel và correction trên LUT là cùng một phép biến đổi; Exposure gain, BW và gamut mapping cũng là phép biến đổi theo màu. Toàn chuỗi có thể nướng thành một LUT.

### Nhánh

Tất cả hậu xử lý giống `joint_mapped` (gain ước lượng lại từ output đã correction so với reference, BW detector, gamut mapping). Cap correction ±0,15 mỗi kênh (encoded), cố định trước.

| Nhánh | Correction |
|---|---|
| joint_mapped | α=0; phải tái hiện Codex: macro 7,542, neutral 7,234 |
| full_a05 / full_a10 | α=0,5 / 1, cả luma và chroma |
| chroma_a05 / chroma_a10 | α=0,5 / 1, chỉ phần chroma (trừ luma của residual) |
| frozen_none | Đóng băng LUT từ cặp (calibration source, reference) không correction — tương ứng `fixed_joint` của Codex nhưng nướng LUT + gamut mapping |
| frozen_full | Như trên với correction full α=1 tính trên calibration source |

Calibration source: giống `Benchmark-ExposureMonochrome.py` (asset train đầu tiên theo id), khóa theo (reference, look). Đây là ảnh train, **không phải** ảnh booth đã hiệu chuẩn.

### Kiểm tra đúng đắn (bắt buộc trước khi báo kết quả)

1. Áp lại hai LUT của model lên source khớp `adapter.prepare` (sai số ≤ 1e-5).
2. α=0 tái hiện macro 7,542 và neutral 7,234 của output-policy.
3. Với α=1 không cap trên 16 ca neutral, MAE raw của (corrected − source) khớp `residual_difference_mae_255` = 10,248 của Codex.
4. Sai số nướng LUT 33³ và 65³ so với chuỗi chính xác: báo mean và max.

### Kỳ vọng và tiêu chí thành công

Kỳ vọng: residual difference sau mapping của Codex (4,76/255) là sàn xấp xỉ cho neutral khi correction đầy đủ, nên gate neutral 3/255 **có thể vẫn trượt**.

Một nhánh correction được coi là có ích nếu đồng thời:
- neutral mapped MAE giảm ít nhất 1,5/255 so với joint_mapped (7,234 → ≤ 5,73);
- không look nào tệ hơn quá 1,0/255;
- macro MAE không tệ hơn joint_mapped.

Báo số ca tốt hơn/tệ hơn, gate raw trước mapping và gain EV từng ca. Nhánh frozen đánh giá cùng tiêu chí, so `frozen_full` với `frozen_none`, đồng thời báo khoảng cách so với correction theo từng ảnh.

## Kết quả

Output `.local/matchlook-data/self-reference-correction-2026-10-13-001/` (summary, cases, raw-cases, ev, plan JSON, HTML + PNG 16 ca đầu). 136,8 giây CPU, 0 optimizer step, state model và hash file nguồn không đổi. Bốn unit test của `Test-SelfReferenceCorrection.py` đều qua.

### Kiểm tra đúng đắn: đều đạt

| Kiểm tra | Kỳ vọng | Đo được |
|---|---|---|
| Áp lại hai LUT so với `adapter.prepare` | ≤ 1e-5 | 0 |
| α=0 | macro 7,542/7,545, neutral 7,234 | 7,545 / 7,234 |
| Neutral α=1 không cap, MAE(corrected − source) | 10,248 | 10,248 |
| Sai số nướng LUT 33³ (mean / max, /255) | báo cáo | 0,16 / 21,4 (none); 0,14 / 18,7 (full) |
| Sai số nướng LUT 65³ | báo cáo | 0,05 / 14,9 (none); 0,04 / 8,8 (full) |

Sai số trung bình khi nướng nhỏ, nhưng sai số lớn nhất khá cao: hai LUT 17³ nối tiếp nhau, cộng thêm gamut mapping và gain lớn, không phải một LUT tam tuyến tính chính xác. Cần kiểm tra trực quan vị trí của các sai số lớn trước khi xuất `.cube`.

### Correction theo từng ảnh (sau mapping, /255)

| Nhánh | Macro | Neutral | Da | Highlight | Tốt hơn/tệ hơn | Vi phạm gate (mapped / raw) |
|---|---:|---:|---:|---:|---:|---:|
| joint_mapped | 7,545 | 7,234 | 7,991 | 13,850 | — | 16 / 74 |
| full_a05 | 6,861 | 6,623 | 7,541 | 9,578 | 80/48 | 16 / 103 |
| full_a10 | **6,803** | **5,172** | 8,828 | **7,242** | 78/50 | **14** / 193 |
| chroma_a05 | 7,414 | 6,808 | 7,309 | 14,027 | 75/51 | 16 / 72 |
| chroma_a10 | 7,570 | 6,825 | 7,460 | 14,389 | 67/61 | 16 / 99 |

Chênh lệch theo look so với joint_mapped:

| Look | full_a05 | full_a10 | chroma_a05 | chroma_a10 |
|---|---:|---:|---:|---:|
| cool | +0,02 | +0,69 | +0,46 | +1,04 |
| film_matte | −1,58 | −1,61 | −0,18 | +0,04 |
| high_contrast | −0,20 | +0,32 | +0,21 | +0,61 |
| low_key | −0,26 | **+1,23** | −0,41 | −0,38 |
| monochrome | −0,36 | −1,28 | +0,02 | +0,04 |
| neutral | −0,61 | **−2,06** | −0,43 | −0,41 |
| pastel | −1,31 | −2,14 | −0,30 | −0,22 |
| warm | −1,17 | −1,10 | −0,42 | −0,52 |

**Theo tiêu chí đã đăng ký: không nhánh nào đạt.**
- `full_a10` đạt mục neutral (−2,06 ≥ 1,5) và macro, nhưng low-key tệ hơn +1,23, vượt ngưỡng 1,0. Da cũng tệ hơn 0,84. Neutral cải thiện ở 14/16 ca, nhưng 2 ca vẫn tệ hơn.
- `full_a05` không làm look nào tệ hơn quá 0,02 và cải thiện macro, nhưng neutral chỉ giảm 0,61, chưa đạt 1,5.
- Chroma-only gần như không tác động tới neutral (−0,4). **Khuyến nghị "chỉ sửa chroma" mà Opus gửi Codex trước đó là sai:** thành phần độ sáng của drift chiếm phần lớn, và một gain Exposure duy nhất không bù được hình dạng của nó.
- Correction full làm số vi phạm raw trước mapping tăng mạnh (74 → 193). Gamut mapping che phần này ở đầu ra, nên không được coi model đã ổn hơn về miền màu.
- Gain Exposure ước lượng lại thay đổi theo correction (ví dụ low-key α=1: −1,00 → −0,61 EV), nên một phần hiệu ứng của nhánh full đến từ việc tương tác với Exposure. Đây là lý do khả dĩ khiến low-key tệ đi.

### LUT đóng băng: chưa cho thông tin

`frozen_none` 53,48 và `frozen_full` 53,76. Hai nhánh này **không phải** bản tái hiện `fixed_joint` của Codex (51,75), vì phạm vi đóng băng khác nhau. Codex vẫn chạy model trên từng ảnh validation và chỉ đóng băng gain. Nhánh ở đây đóng băng toàn bộ chuỗi (hai LUT của model, gain, BW, mapping) và không chạy model trên ảnh validation. Cả hai thất bại vì cùng một nguyên nhân gốc: ảnh calibration là một asset train có độ sáng khác hẳn, nên gain ước lượng trên nó trung bình khoảng +2 EV và làm hỏng mọi ảnh validation. Chênh lệch giữa hai nhánh frozen (+0,28) nhỏ hơn rất nhiều so với lỗi do calibration, nên **không kết luận được** correction có giúp khi đóng băng hay không. Thí nghiệm này chỉ có ý nghĩa với ảnh calibration cùng điều kiện chụp với source, tức là ảnh booth đã hiệu chuẩn hoặc một proxy được chọn có căn cứ.

## Kết luận

1. Self-reference correction đầy đủ có tín hiệu thật trên neutral (7,23 → 5,17) và highlight (13,9 → 7,2), nhưng đánh đổi ở low-key, da và cool. Chưa chọn làm mặc định.
2. Bước tiếp theo, chưa chạy. Mỗi bước phải đăng ký tiêu chí trước:
   - **Trước tiên:** ước lượng gain Exposure trên output **chưa** correction, sau đó mới áp correction. Bước này kiểm tra trực tiếp giải thích "low-key tệ đi do correction làm thay đổi gain".
   - **Sau đó, nếu vẫn cần:** quét α (khoảng 0,5–0,75). Chọn α trên 128 ca phát triển này, sau đó đánh giá **một lần** trên một holdout đã khóa và chưa từng xem. Không quét α trên holdout, vì làm vậy biến holdout thành tập chọn tham số. Chia lại 128 ca đã xem cũng không tạo ra blind test mới (điều chỉnh theo nhận xét của Codex).
   - **Thay thế tạm cho booth trong thí nghiệm LUT đóng băng:** leave-one-source-out (calibration trên một source validation, áp cho bảy source còn lại với cùng reference), cho tới khi có ảnh booth đã hiệu chuẩn.
3. Gate neutral 3/255 vẫn trượt ở 14/16 ca, phù hợp với kỳ vọng về sàn ~4,8.
4. Muốn đánh giá LUT đóng băng cần ảnh calibration cùng điều kiện chụp. Đây là thêm một lý do để chụp bộ ảnh kiểm chuẩn tại booth.

## Tái hiện

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-SelfReferenceCorrection.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-SelfReferenceCorrection.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\self-reference-correction-2026-10-13-001
```

Chọn output mới khi chạy lại. `phase-status.md` chưa được cập nhật, vì Codex đang chỉnh file đó.

## Lượt 2: gain cố định trước correction (tiền đăng ký, ghi trước khi chạy)

Mục đích: cô lập tương tác giữa gain và correction. Gain Exposure và BW được ước lượng trên output **chưa** correction (giống hệt gain của `joint_mapped`), rồi giữ nguyên cho mọi α. Correction full (luma + chroma), cap ±0,15. Không chạy lại nhánh frozen hay chroma.

Nhánh: `joint_mapped` (α=0), `fixed_a025`, `fixed_a05`, `fixed_a075`, `fixed_a10` (gain cố định); `reest_a05`, `reest_a10` (gain ước lượng lại, phải tái hiện full_a05/full_a10 của lượt 1).

Kiểm tra: `joint_mapped` và `reest_*` phải tái hiện lượt 1 (7,545; 6,861; 6,803).

**Dự đoán kiểm chứng giả thuyết:** nếu low-key tệ đi ở lượt 1 là do gain bị ước lượng lại, thì `fixed_a10` có low-key tệ hơn joint_mapped không quá 1,0/255. Nếu vẫn vượt 1,0, giả thuyết bị bác bỏ.

**Tiêu chí đạt** (giữ nguyên lượt 1): neutral giảm ≥ 1,5; không look nào tệ hơn > 1,0; macro không tệ hơn. **Tiêu chí phụ khai báo trước:** MAE da không tệ hơn quá 0,5/255. Lượt 1 không có tiêu chí này; nó được thêm vào vì booth chụp chân dung, và được ghi trước khi chạy lượt 2.

**Quy tắc chọn α trên dev:** trong các α `fixed_*` đạt cả tiêu chí chính lẫn tiêu chí phụ, chọn α có neutral giảm nhiều nhất. Nếu không α nào đạt, không chọn. α được chọn ở đây chỉ là lựa chọn trên dev, chưa phải kết quả kiểm chứng.

## Lượt 2: kết quả

Output `.local/matchlook-data/self-reference-correction-2026-10-13-002/`. 61,6 giây CPU, 0 optimizer step, state model và hash không đổi. `joint_mapped` (7,545) và `reest_a05`/`reest_a10` (6,861 / 6,803) tái hiện đúng lượt 1. Chain sai số 0, residual neutral 10,248.

| Nhánh | Macro | Neutral | Da | Highlight | Tốt hơn/tệ hơn | Vi phạm (mapped / raw) |
|---|---:|---:|---:|---:|---:|---:|
| joint_mapped | 7,545 | 7,234 | 7,991 | 13,850 | — | 16 / 74 |
| fixed_a025 | 8,573 | 8,407 | 8,528 | 14,916 | 31/97 | 16 / 61 |
| fixed_a05 | 11,207 | 11,249 | 11,002 | 16,436 | 4/124 | 16 / 65 |
| fixed_a075 | 14,323 | 14,480 | 14,356 | 18,179 | 1/127 | 16 / 79 |
| fixed_a10 | 17,631 | 17,767 | 17,953 | 20,071 | 0/128 | 16 / 99 |
| reest_a05 | 6,861 | 6,623 | 7,541 | 9,578 | 80/48 | 16 / 103 |
| reest_a10 | 6,803 | 5,172 | 8,828 | 7,242 | 78/50 | 14 / 193 |

Với gain cố định, **mọi look đều tệ đi**, và tệ hơn đơn điệu khi α tăng. Ở α=1, look tệ đi ít nhất là cool (+7,95), nhiều nhất là high_contrast (+12,52); low-key tệ hơn +8,14.

**Theo đăng ký trước:**
- Dự đoán kiểm chứng (`fixed_a10` có low-key tệ hơn không quá 1,0) **thất bại** (+8,14). Giả thuyết "low-key tệ đi do gain bị ước lượng lại" bị bác bỏ ở dạng đã phát biểu.
- Không α `fixed_*` nào đạt tiêu chí chính hoặc tiêu chí da, nên **không chọn α nào**. `reest_a05` đạt tiêu chí da (−0,45) nhưng vẫn trượt tiêu chí neutral (−0,61 < 1,5), giống lượt 1.

**Diễn giải (chưa kiểm chứng):** drift self-reference chủ yếu là độ sáng. Output self sáng hơn source khoảng +12 đến +17/255 ở cả ba kênh (báo cáo self-reference của Codex). Trừ drift đầy đủ làm cả ảnh tối đi. Khi gain cố định, phần tối này cộng dồn với gain vốn đã làm tối (ví dụ low-key −1,0 EV), nên mọi look đều hỏng. Khi gain được ước lượng lại, phần dịch sáng toàn cục được bù lại, và cái còn tác dụng là **hình dạng** của drift (khác biệt theo vùng tone và màu). Vậy correction và Exposure không tách rời được theo cách đã thiết kế: lợi ích của `reest_*` đến từ tổ hợp của hai bước.

**Đề xuất tiếp theo (chưa chạy, phải đăng ký trước):** correction đã chuẩn hóa exposure. Tách khỏi drift `r = M_self(x) − x` thành phần gain toàn cục của nó (EV self = log2 của median luminance tuyến tính output self chia cho source, không dùng target), chỉ trừ phần còn lại, rồi áp gain ước lượng một lần như `joint_mapped`. Cách này biến "phần hình dạng" thành một tham số tường minh thay vì dựa vào việc ước lượng lại gain. Chọn α trên dev; kiểm chứng trên nhóm ảnh mới độc lập.

## Tái hiện lượt 2

```powershell
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-SelfReferenceCorrection.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\self-reference-correction-2026-10-13-002 --arm-set v2 --skip-frozen
```

Mặc định `--arm-set v1` tái hiện lượt 1, kể cả nhánh frozen.
