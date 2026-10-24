# Hai bước chuẩn bị fine-tune — hoàn tất 12/10/2026

## Kết luận

Pack hoạt động: `.local/matchlook-data/finetune-prep-2026-10-12-002`. Đã sẵn sàng về kỹ thuật cho **thử nghiệm development học look tổng hợp trên JPEG**, chưa sẵn sàng phát hành model hay cam kết khôi phục camera preset/giống90%. Chưa fine-tune CanonCGT, optimizer step0, weights hash không đổi. Không cần RAW để thực hiện thử nghiệm này.

Pack001 là bản trung gian và đã đặt preparation_ready/training_readyfalse sau khi phát hiện cần giữ thêm historical test. Không dùng pack001 để train.

## Bước1 — dữ liệu và phân chia

Kiểm hash192ảnh nguồn; giữ file split85/15 cũ nguyên vẹn bằng hash. Bản mới phân chia theo nhóm: session01 + cả16ảnh mới vào train; session02 validation; session03 test. Vì chưa xác nhận session của16ảnh mới, giữ toàn bộ16ảnh thành một nhóm bảo thủ, không tách cặp/nhân vật sang split khác.

Phân bổ trước cách ly:162ảnh development pool và30test =84.375%/15.625%, gần85/15 nhất theo các nhóm này. Sau đó giữ20ảnh từng ở test cũ, nhưng không ở session03, trong quarantine để không tái sử dụng làm train/validation.

| Vai trò cuối | Số ảnh |
| --- | ---: |
| Train | 94 (gồm cả16JPEG mới) |
| Validation | 48 |
| Test development dự trữ | 30 |
| Quarantine historical test | 20 |
| Tổng | 192 |

Không gọi đây là đúng85/15 trên số ảnh đang dùng: nếu loại quarantine,142development và30test là82.56%/17.44%. Ưu tiên bảo toàn historical test và cả nhóm hơn ép tỷ lệ. Validation nằm trong development pool, không lấy từ test. DHash64bit <=10 giữa các split không tìm thấy candidate; điều này không chứng minh danh tính độc lập hoặc không còn near-duplicate. Test đã từng thấy trong development, không phải benchmark mù mới.

Code: Prepare-FinetunePack.py lines10/41/45 giữ historical test; FinetuneCurriculum.py lines14/20 kiểm tra group/parent/reservation. Mọi example chỉ có split train hoặc validation, không tạo test example cho trainer.

## Bước2 — target/augmentation/loader/preflight

8công thức encoded-sRGB từ PhaseBData: neutral, warm, cool, pastel, film_matte, high_contrast, low_key, monochrome. Source sạch là JPEG đã render. Target = công thức áp lên source sạch; reference = cùng công thức áp lên ảnh khác nội dung, cùng split. Ưu tiên khác group trong train nếu có. Không dùng reference bất kỳ như paired target; target có công thức biết trước nhưng chỉ là look tương đối trên các base JPEG khác nhau. Base JPEG/preset khác nhau vẫn có thể gây confounding; chưa giải quyết disentanglement phong cách thực.

Mỗi train source x8looks x3variants =2256mẫu:

- clean;
- EV-0.4, gains(0.95,1,1.05), JPEG90 4:2:0;
- EV-0.6, gains(1.1,1,0.9), JPEG70 4:2:0.

Mỗi validation source x8looks sạch =384mẫu, không augmentation selection set. Tổng2640mẫu được loader kiểm tra. Augmentation cố định/tái lập, không giả lập mọi camera/nguồn sáng; chưa có positiveEV training và không cố khôi phục clipping. Mọi augmentation kiểm không mất dữ liệu trước codec. Reference giữ nguyên phong cách công thức, không WB-neutralize.

Neutral clean dùng identity loss. Neutral bị corruption có target sạch nhưng **không dùng identity-to-corrupted-source loss**; dùng reconstruction/skin để học bỏ corruption. Monochrome target kiểm achromatic. Đây là chi tiết tránh tạo loss mâu thuẫn.

Mask: FaceLandmarker + semantic face-skin class3, confidence0.7, bỏ mắt/môi/mày, erosion, spatial support, overlap rejection và consistencyIoU>=0.65 giữa source/darkened. Predicted masks không phải ground truth.142train/validation sources có139safe mask,3zero fallback; zero mask bỏ skin loss, vẫn global reconstruction. Mask source512được resize nearest theo ảnh256. Mask002 được tái sử dụng từ source/hash giống pack001, không giả là detector chạy lại. Không sử dụng nhận dạng danh tính.

Loader kiểm hash source/mask và recipe manifest phải trùng implementation. PreparedCurriculum dùng LRU64cache, chỉ cho phép head-only tuning vì cached features cần backbone frozen. Không cache toàn bộ2256mẫu. Run entry shuffle metadata bằng seed trước train; seed17, model eval giữ BN frozen. Memory/latency NUC thật chưa benchmark.

## Preflight bản cuối đã chạy

- Kiểm đủ2640mẫu, target shape/finite/neutral/BW hợp lệ.
- Forward+backward24tổ hợp look/augmentation: loss/gradient hữu hạn, grad norm0.8086–2.7197 trước clip. Không gọi optimizer step.
- Kiểm thêm3ảnh mới: square neutral bị corruption, nhóm nhiều mặt warm và ảnh thiênmonochrome. Shape/forward/backward đều PASS.
- Full384validation với SSL gốc: macro look MAE(clamped)21.3730/255; chưa đạt quality gate,182case-metric violations, không phải182ảnh hay182lỗi độc lập.
- Hash state trước/sau `a4ecb944efbc7e74bb9faa6d9af88ba84ee5e413008c6f16f7ee7cba1e8d9fe1` không đổi. Grad được clear, optimizer step0. Preflight chính81.712sCPU4threads; không tính thêm geometry checks và mask preprocessing.
-30automated tests PASS: curriculum9, losses7, quality5, harness3, loop3, pipeline3. Một số unit test dùng optimizer trên fixture nhỏ để kiểm cơ chế; không fine-tune CanonCGT.
- Plan-only entry PASS; thử --execute khi training_allowedfalse bị từ chối trước model training và không tạo thư mục output.

Baseline gate fail **không đồng nghĩa code preflight fail**. Gate vẫn giữ các engineering thresholds cũ, chưa calibration; không nới để cho pretrained checkpoint pass. Candidate sau fine-tune chỉ lưu best khi đủ gate và MAE cải thiện, còn last dùng để chẩn đoán. Có khả năng run không có best checkpoint hợp lệ; không được gọi last là model đạt chất lượng.

## Cấu hình sẵn sàng, chưa thực thi

Official SSL_updated_251111, chỉ Restyler.generate_lut.outc4355params. Adam1e-4, gradient clip1, tối đa30epochs, patience5, minDelta0.1/255, loss reconstruction1/skin0.5/neutral0.5/monochrome0.25/highlights0.1/gamut0.1/smoothness0.01. Learning rate/weights là starting values, chưa tối ưu bằng training.

`run-config.json` có training_allowedfalse. `curriculum.ready.json` training_readytrue chỉ nói pack đủ cho development pilot, production_training_readyfalse. Trước lần train phải thông báo người dùng, bật cấu hình có chủ ý và chọn output mới; không tự phát train trong bước chuẩn bị.

Ablation design trong ablation-plan.json: so clean repeated3 lần với3augmentation cùng step budget, seed/model/validation; so correction oracle ở evaluation riêng. Chưa chạy hai training arms; causal benefit augmentation chưa có kết quả. Primary run hiện chuẩn bị augmentation candidate, không ngầm lên lịch nhiều lần fine-tune.

## Xem và tái lập

- sources.json: phân chia/group/quarantine/hash.
- mask-report.json: predicted ROI provenance và fallback.
- curriculum.ready.json: sample specs/recipes/source/ref IDs.
- preflight-report.json +extra-preflight.json: loss/gradient/validation/zero-step evidence.
- readiness-summary.json và index.html: tóm tắt và4ca minh họa Source/Reference/Target. Minh họa là dữ liệu học tổng hợp, **không phải ảnh AI output**. curriculum-overview.jpg đã xem; HTML chưa browser QA.

Scripts từ repo: Prepare-FinetunePack.py, Prepare-FinetuneMasks.py, FinetuneCurriculum.py, Preflight-PreparedFinetune.py và Run-PreparedFinetune.py. Các builder yêu cầu output chưa tồn tại; không ghi đè pack nguồn. Mask002 được copy với hash thay vì rerun. Plan-only:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Run-PreparedFinetune.py
```

Không có optimizer step trong lệnh trên. Chuẩn bị đã xong; bước kế tiếp là thông báo kế hoạch/thời gian và chạy fine-tune development có giới hạn, không thêm yêu cầu RAW/bảng màu vào điều kiện chặn pilot này.
