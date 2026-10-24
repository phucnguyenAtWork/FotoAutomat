# Đối chứng ngắn objective monochrome và gamut

## Phạm vi được thực thi

Sau yêu cầu tiếp tục, đã thông báo trước khi chạy thử optimizer. Đây là thí nghiệm phát triển, không phải fine-tune dài hoặc model được duyệt. Không đổi `MatchLookLosses.py`, runner fine-tune chính, gate hoặc WPF. Các loss thử nghiệm nằm riêng trong `CandidateLookLosses.py`.

4 nhánh cùng checkpoint LAST, Adam mới, seed 17, learning rate 0,0001, clip norm 1 và cùng thứ tự 96 mẫu: 8 source train × 4 look chẩn đoán × 3 augmentation hiện có. Reference train lấy từ group train khác. Validation giữ riêng: 8 source × 8 look × 2 reference = 128 ca, có cả look không xuất hiện trong minibudget train để theo dõi hồi quy. Test không dùng.

## Các nhánh

| Nhánh | Thay đổi |
|---|---|
| baseline | Objective và hệ số hiện tại |
| bw_l1 | Thay monochrome channel variance bằng mean(max RGB − min RGB) |
| gamut_tail | Thay gamut squared mean bằng mean L1 excursion của 0,1% giá trị kênh vượt miền lớn nhất |
| joint | Cả hai thay đổi |

Gamut tail dùng tolerance 1e-6, chọn ít nhất một giá trị kênh; không tự clamp prediction trong loss. Tail loss hướng về lỗi hiếm nhưng không phải phép thay thế chính xác của gate tỷ lệ/cực đại. BW L1 chỉ bật khi target là monochrome; các loss khác giữ nguyên.

Hệ số candidate dùng median tỷ lệ gradient trên **train calibration**, nhắm norm bằng 10% reconstruction, chặn trong [0,0001; 1]. Calibration có 24 ca clean từ 8 source train và 3 look; chỉ ca có gradient candidate khác 0 được dùng tính tỷ lệ, lưu số liệu từng ca. Mục tiêu 10% và giới hạn là heuristic thí nghiệm, không phải hệ số tối ưu đã được chứng minh. Không lấy tỷ lệ 122 lần từ validation để nhân trọng số.

Mỗi candidate thay cả dạng loss và hệ số calibration của term đó, nên nếu cải thiện chưa thể tách nguyên nhân do dạng hay hệ số. Muốn kết luận nhân quả sâu hơn cần nhánh same-form/reweighted riêng. Giới hạn 96 bước, một seed và Adam reset cũng không đủ để kết luận kết quả hội tụ hay tái hiện full fine-tune.

## Tái hiện

Từ FotoAutomat; chọn output mới nếu chạy lại:

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-CandidateLookLosses.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-LossAblation.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --plan .local\matchlook-data\reference-consistency-2026-10-12-001\plan.json --output .local\matchlook-data\loss-ablation-2026-10-12-001 --execute
```

Đầu ra lưu experiment/coefficients/hash, validation từng ca, train history, ảnh và head state từng nhánh. Head state không phải model standalone: cần base LAST có đúng hash được ghi. Kiểm tra backbone không đổi sau từng nhánh và checkpoint/config/manifest/plan nguồn không đổi sau toàn bộ thí nghiệm. Không chọn hoặc triển khai checkpoint tự động.

Ba kiểm thử loss đã qua: gray/cast, sparse gamut excursion có gradient đẩy vào miền, safe RGB không bị phạt.

## Trạng thái

Đã hoàn tất trong 265,3 giây trên CPU hiện tại. 96 optimizer steps/nhánh, tổng 384 steps. Backbone từng nhánh và checkpoint/config/manifest/plan nguồn giữ nguyên; các head thử nghiệm lưu riêng. Không dùng test, không triển khai vào app.

Calibration cho BW L1: 0,048694 (8 ca active); gamut tail: 0,228585 (11 ca active trong 24 ca calibration). Không so trực tiếp các hệ số này với hệ số loss cũ vì dạng toán học thay đổi.

## Kết quả trên cùng 128 ca validation

| Trạng thái/nhánh | Macro MAE /255 | Vi phạm ca–tiêu chí | Đạt gate |
|---|---:|---:|---|
| LAST trước thử nghiệm | 17,600 | 134 | Không |
| Baseline, 96 bước | 17,831 | 101 | Không |
| BW L1, 96 bước | 17,823 | 101 | Không |
| Gamut tail, 96 bước | 17,898 | 90 | Không |
| Joint, 96 bước | 17,892 | 90 | Không |

Baseline và BW L1 đều có 29 vi phạm tỷ lệ RGB, 40 vi phạm excursion, 16 neutral, 16 BW. Gamut/joint có lần lượt 26, 32, 16, 16. Số vi phạm không phải số ảnh lỗi. Mean raw monochrome chroma giảm nhẹ: baseline 9,416, BW 9,320, gamut 9,152, joint 9,069/255; vẫn lớn hơn gate 2/255 và cả 16 ca BW đều không đạt.

Low-key vẫn suy giảm: MAE baseline 47,097, joint 47,815/255. Chỉ thay BW hầu như không đổi kết quả so baseline. Gamut tail giảm lỗi miền RGB thêm so baseline, nhưng MAE tổng và low-key tệ hơn. Cả bốn nhánh đều có macro MAE cao hơn checkpoint LAST ban đầu trên đúng tập ca này.

Không chọn nhánh thắng để train dài, không nới gate. Thí nghiệm cho thấy sửa hai loss theo cấu hình này chưa đủ. Chưa bác bỏ mọi dạng BW/gamut loss; một seed và 96 bước không chứng minh hiệu quả khi hội tụ. Không gán chênh lệch riêng cho dạng loss vì candidate thay cả hệ số.

## Bước nghiên cứu kế tiếp

Ưu tiên kiểm tra tín hiệu style và khả năng biểu diễn của head: đối chiếu khả năng fit một look low-key riêng với nhiều look dùng chung head, giữ source/reference và target rõ ràng. Nếu chỉ một look cũng không fit được, cần xem canonicalization/head trước khi tăng dữ liệu hoặc PPL. Nếu single-look fit được nhưng multi-look không, cần kiểm tra conditioning/style supervision và cách tối ưu. Đây là đề xuất chưa thực thi; chưa mở backbone hoặc bắt đầu full fine-tune mới.
