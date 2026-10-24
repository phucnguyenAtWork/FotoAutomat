# Audit gradient của head tạo LUT

## Phương pháp

Đo tại SSL gốc và checkpoint LAST của pilot 12/10/2026. 8 source validation × 4 look × 1 reference × 3 điều kiện (clean và hai augmentation đúng config train) = 96 ca/model. Dùng plan của benchmark chẩn đoán; không dùng test, không đổi recipe, loss, gate hoặc weights.

Head 4.355 tham số là phần duy nhất cho phép đạo hàm. Backbone/BN giữ eval và đóng băng. `CanonAdapter.prepare` tạo feature/canonical dưới no_grad; `CanonAdapter.forward` dựng graph mới qua head. Với từng loss đã nhân hệ số config, `torch.autograd.grad` đo vector gradient; kiểm tra tổng vector khớp gradient total objective. Không gọi optimizer, không ghi vào `.grad`, không gradient clipping.

Các phép đo:

- Norm từng weighted loss và total; số ca total norm > 1 để biết ca nào sẽ bị clip trong training.
- Cosine gradient từng loss với total. Cosine null khi norm gần 0; không biến thành 0 hay đồng thuận giả.
- Cosine giữa gradient total của hai look trên cùng source/reference/augmentation.
- Cosine clean so với từng augmentation trên cùng source/reference/look.

Báo mean cosine của các cặp từng ca và số cặp có cosine âm, không phải cosine giữa hai gradient trung bình. Norm ratio không phải phần trăm đóng góp và có thể vượt 1 do triệt tiêu vector.

Cosine âm chỉ là xung đột cục bộ bậc nhất tại checkpoint đo. Không chứng minh xung đột gây ra lỗi của cả quá trình train; Adam moments, thứ tự mẫu và clipping có thể thay đổi update thực tế. Audit không kết luận capacity của backbone nếu mở train, và không chứng minh PPL sẽ sửa lỗi.

## Tái hiện

Từ FotoAutomat, chọn output mới nếu chạy lại:

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-GradientAudit.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Audit-HeadGradients.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --plan .local\matchlook-data\look-diagnostics-2026-10-12-001\plan.json --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --output .local\matchlook-data\gradient-audit-2026-10-12-001
```

Đầu ra: plan và hash, summary, JSON từng ca/model, index.html. Kiểm tra state hash trước/sau và hash checkpoint/config/manifest/plan không đổi. Hai kiểm thử đã qua: đối nghịch/zero cosine; gradient có trọng số cộng đúng và không sửa tham số hoặc grad buffer.

## Trạng thái

Đã hoàn tất trong 49,6 giây: 96 ca/model, 0 optimizer steps. State model, file checkpoint, config, manifest và plan giữ nguyên; grad buffer không thay đổi. Chưa fine-tune thêm.

## Kết quả chính

### Không xác nhận giả thuyết augmentation kéo ngược low-key ở LAST

Clean so với `dark_cool_jpeg90`: mean cosine 0,853, 0/8 cặp âm. Clean so với `dark_warm_jpeg70`: 0,897, 0/8 cặp âm. Ở SSL gốc, nhánh warm có 2/8 cặp âm. Không được lấy một giả thuyết trước đó làm kết luận: tại LAST được đo, low-key clean/aug chủ yếu cùng hướng dù đầu ra vẫn sai. Hai snapshot không mô tả toàn bộ đường đi của training.

### Gradient BW và gamut nhỏ so với reconstruction

Mean norm từng weighted gradient ở ca clean của LAST:

| Look | Reconstruction | Skin | Loss chuyên biệt |
|---|---:|---:|---|
| Monochrome | 1,48253 | 0,71745 | Monochrome 0,01215 |
| Neutral | 1,71213 | 1,03864 | Gamut 0,00149; neutral 0,85606 |
| Film-matte | 1,64131 | 0,92293 | Gamut 0,000328 |
| Low-key | 2,28090 | 1,13421 | Gamut 0,000000661 |

Monochrome khoảng 122 lần nhỏ hơn reconstruction về norm trung bình; neutral gamut khoảng 1.147 lần nhỏ hơn reconstruction. Đây không phải phần trăm đóng góp hay bảo đảm đổi hệ số sẽ sửa được gate. Gamut phụ thuộc mức/tỷ lệ excursion và dùng bình phương trung bình, còn gate dùng cả tỷ lệ và cực đại; hai tiêu chí không cùng dạng.

Neutral clean có target bằng source nên neutral loss hiện tại lặp lại cùng hướng reconstruction với hệ số 0,5; không phải một cơ chế học identity độc lập. Augmented neutral tắt term identity theo hợp đồng để tránh target sai. Cần ghi nhận khác biệt weighting này trong ablation.

### Xung đột giữa look có tồn tại nhưng chưa đủ kết luận nhân quả

Ở LAST clean: film-matte vs monochrome có 6/8 cặp âm, mean cosine -0,192; low-key vs monochrome cũng 6/8 âm nhưng mean chỉ -0,010. Neutral vs monochrome 5/8 âm, mean -0,050. Không chọn thuật toán xử lý xung đột chỉ từ tỷ lệ âm; cần xét mức, norm, error và ablation.

Toàn bộ 32 ca clean của mỗi model có total gradient norm > 1; trainer hiện tại sẽ clip norm ở các ca này. Clipping giữ hướng vector total nhưng đổi độ lớn trước Adam; audit chưa mô phỏng optimizer state. Không quy clipping là nguyên nhân thất bại.

## Đề xuất trước lần train kế tiếp

1. Dựng ablation loss/gate: giữ cùng data, seed, backbone, số bước; kiểm tra riêng objective monochrome và gamut có phù hợp metric nghiệm thu. Đo gradient trước khi chọn hệ số, không nhân cố định theo tỷ lệ norm trên một nhóm.
2. Đối chứng head-only hiện tại với nhánh có tín hiệu style/recipe rõ hơn. Chưa coi PPL là thuốc sửa low-key; embedding hiện đóng băng nên phải thiết kế lại nếu muốn học consistency.
3. Dùng benchmark màu và consistency hiện có để quyết định; không nới gate hay triển khai LAST. Chưa chạy optimizer hoặc tự train lần nữa trong audit này.
