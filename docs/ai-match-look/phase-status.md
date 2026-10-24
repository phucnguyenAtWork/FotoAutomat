## 13/10/2026 — trạng thái hiện tại: có ứng viên correction trên dev, chưa đạt quality gate

**Bổ sung phân bố da:** Opus báo norm α=1 có41/128ca tăng lỗi da>2/255, max+10,63; trung bình+0,047 che các hồi quy này. Giữ α=1 là ứng viên đã chọn, không coi da đã ổn định. Đã ghi [dự đoán excursion trong mask da và protocol headroom](headroom-limited-protocol-2026-10-13.md) trước khi đo: so nhóm da xấu đi với nhóm còn lại, raw baseline và correction, xét từng look/source để tránh nhiễu. Cool16/16 cùng nhóm nên không có đối chứng A/B nội bộ. Chưa chạy phép đo này hoặc limiter; chưa chọn correction theo vùng da. Các số phân bố Opus cung cấp sẽ được tái tính trong audit.

**Đối chứng headroom bổ sung trước chạy:** so limiter với correction thu nhỏ đều cùng độ lớn output trước mapping, khớp không dùng target: một β chung và β_i theo từng ảnh. Chỉ công nhận lợi ích riêng nếu vượt đối chứng matched về da và các look cool/low_key/high_contrast theo protocol;≤30ca tự nó chưa đủ. Báo vị trí trên đường đánh đổi neutral–số ca da tăng lỗi>2 của norm α=0,25…1. Chưa chạy matching/limiter, giữ α=1, không chọn lại theo điểm dev.

**Cập nhật mới nhất:** [Correction chuẩn hóa exposure](exposure-normalized-correction-results-2026-10-13.md) đã chạy128ca trong36,24giây CPU, không train. Protocol khóa trước: α=0,5/0,75/1 đạt tiêu chí dev; chọn α=1 theo neutral thấp nhất. Macro7,545→6,371; neutral7,234→4,528; highlight13,850→6,838. Low-key tăng0,401 và da tăng0,047, trong giới hạn đã đăng ký. Raw violations74→181; sau mapping vẫn13neutral lỗi. **Chưa đạt quality gate, chưa phát hành.** Ba kiểm thử qua; ba đối chứng tái hiện metric tuyệt đối; test không dùng. Protocol và kết quả được lưu riêng. Các mục correction trước đó bên dưới mô tả phiên bản chưa chuẩn hóa exposure, không phải kết quả lượt mới.

Các mục phía dưới là lịch sử theo thời điểm, không phải trạng thái hiện tại. Pilot fine-tune đã chạy 5 epoch; LAST chưa đạt quality gate, không có checkpoint được duyệt để triển khai. Các lượt chẩn đoán/correction ngày 13/10 chỉ suy luận, không train thêm. Xem [pilot](finetune-run-2026-10-12.md), [output policy](output-policy-2026-10-13.md) và [self-reference audit](self-reference-2026-10-13.md).

### Self-reference correction — tiếp nhận kết quả Opus

Đã đọc [báo cáo ablation](self-reference-correction-2026-10-13.md) và đối chiếu `summary.json` tại `.local/matchlook-data/self-reference-correction-2026-10-13-001/`. Phép chạy do Opus thực hiện: 128 ca, 136,8 giây CPU, 0 optimizer step; không dùng test. Đây là tiếp nhận/đối chiếu artifact, không phải Codex chạy lại thí nghiệm. Báo cáo ghi bốn unit test qua; artifact xác nhận parity chuỗi LUT bằng 0, baseline mapped MAE 7,545 / neutral 7,234 và uncapped residual MAE 10,248.

**Không nhánh nào đạt tiêu chí đăng ký trước; không chọn correction làm mặc định.**

| Nhánh | Macro MAE /255 | Neutral | Da | Highlight | Gate mapped / raw |
|---|---:|---:|---:|---:|---:|
| Joint mapped | 7,545 | 7,234 | 7,991 | 13,850 | 16 / 74 |
| Full α=0,5 | 6,861 | 6,623 | 7,541 | 9,578 | 16 / 103 |
| Full α=1 | 6,803 | 5,172 | 8,828 | 7,242 | 14 / 193 |

Full α=1 cải thiện neutral và highlight nhưng low-key tệ hơn +1,23/255, vượt giới hạn +1,0 đã đăng ký; da tệ hơn +0,84. Full α=0,5 chưa giảm neutral đủ 1,5. Chroma-only chỉ giảm neutral khoảng 0,4: ghi nhận Opus rút khuyến nghị này. Mapped RGB hợp lệ không thay thế raw gate; raw violations của full α=1 tăng 74→193.

**Frozen LUT:** MAE frozen_none 53,48 / frozen_full 53,76. Khác nhánh fixed_joint trước đó: lần này đóng băng cả hai LUT model và hậu xử lý, không chỉ gain. Mẫu train khác điều kiện chụp tạo gain khoảng +2 EV là yếu tố gây nhiễu lớn, chưa có đối chứng đủ để quy toàn bộ thất bại riêng cho calibration hoặc kết luận correction vô ích khi đóng băng. Sai số nướng 65³ mean khoảng 0,04–0,05/255 nhưng max tới 8,8–14,9/255; chưa đủ điều kiện xuất `.cube` production.

### Audit ứng viên holdout — đã hoàn tất 13/10/2026

[Kiểm tra lịch sử validation](validation-exposure-2026-10-13.md): plan 128 ca dùng 24/48 validation asset, nhưng **24 asset còn lại đều có execution witness trong history của cả 5 epoch fine-tune** (384 ca/epoch, đủ 48 source và 48 reference). Manifest snapshot trùng hash manifest hiện tại; 48/48 hash ảnh validation đã xác minh. Không khóa 24 ảnh này làm never-used holdout. Giữ nguyên 30 test, không đọc pixel test hoặc đổi split. Có thể tiếp tục chọn α như dev tuning; kiểm chứng độc lập cần nhóm dữ liệu mới chưa tham gia phát triển, hoặc nghiệm thu cuối một lần sau khi đóng băng phương pháp. Audit này không chứng nhận toàn bộ lịch sử test.

### Bước tiếp theo đã đề xuất — chưa thực thi

1. **Cô lập tương tác gain/correction:** lấy `g0` từ output chưa correction và reference, giữ cùng `g0` giữa các α, rồi áp `Dα = M_ref − α·cap(M_self − source)` trước gain/BW/mapping. So với nhánh tính lại gain sau correction đang có. Phân biệt thứ tự *ước lượng gain* với thứ tự *áp gain*; không đổi cả hai trong cùng đối chứng.
2. Đăng ký trước α, cap, tiêu chí neutral/macro/per-look và cách báo da/highlight/raw gamut. Không nới gate để chọn nhánh thắng. Chưa quét α hoặc mở canonicalizer để fine-tune.
3. **Chọn α trên development/calibration, đánh giá một lần trên holdout đã khóa trước.** Không quét/chọn α dựa vào điểm holdout rồi gọi đó là kết quả độc lập. Cả 128 ca hiện tại đã dùng nhiều lần; chia lại chúng không tạo ra blind holdout mới. Nếu chưa có nhóm chụp/subject độc lập chưa tham gia lựa phương pháp, ghi rõ thí nghiệm chỉ có giá trị dev; giữ nguyên tập test đã dành riêng, chưa mở test trong cập nhật này.
4. Leave-one-source-out với reference cố định có thể là proxy chẩn đoán khả năng chuyển LUT; không tương đương hiệu chuẩn booth hoặc holdout độc lập nếu cùng phiên/subject. Không chọn ảnh calibration theo điểm target validation.

Người dùng đã chốt khóa Exposure/WB/flash và dùng đèn liên tục cùng flash. Việc còn thiếu là kiểm chứng ảnh cùng cấu hình booth, không phải hỏi lại quyết định phần cứng. Chưa tích hợp các policy thử nghiệm vào WPF, chưa có model/preset được duyệt production.

## 2026-10-09 — preparation only, no new training

[Preparation report](pretraining-preparation-2026-10-09.md): seven loss tests pass; 176 input hashes verified. Cross-content provenance, subject grouping and a new locked holdout remain unresolved. Notify user before any further fine-tuning. Configuration is draft only.

## 2026-10-08 — head-only fine-tune smoke completed

See [experiment and limitations](finetune-smoke-2026-10-08.md). Two actual SSL head fine-tunes ran with frozen-body integrity checks. Test error improved but validation did not consistently improve; no production promotion. Same-content diagnostic only; cross-content fine-tuning remains pending.

# Tiến trình CanonCGT theo phase

08/10/2026: [Region stability gates](region-stability-2026-10-08.md): stage diagnostics, coverage/spatial/overlap gates, six perturbations per input and fail-closed regional mask routing implemented in Python audit. Six tests PASS. Not WPF integration or improved model weights; unreliable masks are excluded.

08/10/2026: [Region audit](region-audit-2026-10-08.md) executed CPU Face Landmarker + Selfie Multiclass on 12 distinct sources and references (22 unique inputs), 31 source faces; skin ROI/highlight reports and overlay review ready. 3 metric tests PASS. Masks and reference–target consistency still pending, no accuracy claim, grading/fine-tuning unchanged.

08/10/2026: [LUT algorithm prototype](reference-lut-prototype-2026-10-08.md) implemented tetrahedral CUBE I/O, global SWD fitting, regularization and monotone RGB curves; 5 tests PASS, 12-case ablation. Global variants failed visual/MAE quality, not promoted to WPF. Semantic masks/normalization/fine-tuning still pending. Human review imported: SSL 4 đạt/12 cần chỉnh/70 không đạt, E2E 7/13/66, 10 pending each.

07/10/2026: [Portrait model review](portrait-model-review-2026-10-07.md) đã chạy 96 cases × SSL/E2E = 192 outputs CPU, có gallery TARGET/model để duyệt. Người dùng chấp nhận palette chân dung; model review còn pending. Thấy lỗi highlight/low-key/monochrome, chưa train hoặc đạt chất lượng.

07/10/2026: [Phase B pilot](phase-b-results-2026-10-07.md) có 176 source chuẩn hóa, 96 triplets / 8 controlled looks và trang duyệt. Người dùng xác nhận ba buổi riêng, JPEG đã có preset. Đã ghi nhận vai trò/group; giữ diagnostic_only do chưa kiểm tra overlap và khóa holdout. 368 artifacts kiểm chứng, 4 tests PASS; chưa duyệt targets, chưa train. Các mục ngày cũ bên dưới là lịch sử tiến trình.

07/10/2026: đã chạy [Phase A CPU audit](phase-a-results-2026-10-07.md): strict-load E2E/SSL, identity/two-stage LUT, demo-equivalent loader parity, 8 official pairs/checkpoint và 24 booth tone probes. SSL tái lập gần trùng bundled outputs; quality gate còn mở (monochrome còn màu). Chưa train/ONNX; Phase B cần targets người duyệt.

Cập nhật ảnh riêng 06/10/2026: người dùng chấp thuận hướng train và xác nhận quyền batch CustomerImages. Đã kiểm kê 176 assets; thiết lập [Phase A/B](phase-ab-setup.md), roles/groups/targets còn cần duyệt. Không gộp quyền ảnh riêng với Open Images; chưa train.

Tải Open Images 06/10/2026 hoàn tất: 974/974 ảnh, 0 lỗi, ~277 MB; 21 trang gallery tại `.local/matchlook-data/openimages-review-001`. Chưa duyệt toàn bộ chất lượng/quyền và chưa train; xem [báo cáo](openimages-selection.md).

Cập nhật lọc Open Images 06/10/2026: đã chọn 974 ứng viên metadata (700 portrait, 274 cảnh) từ bốn CSV của người dùng. Xem [bộ lọc và kết quả](openimages-selection.md). Chưa tải pixels, duyệt ảnh/quyền hoặc train.

Ngày 05/10/2026. Các phase dưới đây là bước triển khai của riêng AI Match Look, không thay thế roadmap sản phẩm trong AGENTS.md.

Quyết định 06/10/2026: chốt ASUS NUC 13 Pro Slim i7-1360P / RAM 16 GB, không yêu cầu GPU rời làm cấu hình mục tiêu. Xem [triển khai CPU và kiểm chứng](cpu-deployment-target.md). Công cụ CPU đã hỗ trợ warmup/repeats, median/p95 và peak RAM tiến trình Windows; chưa chứng nhận trên NUC thật.

Cập nhật 06/10/2026: đã chạy [preprocessing audit v1](preprocessing-review-2026-10-06.md) trên 15 ảnh PST50, tạo bảng 40 trường duyệt và previews; 3 tests PASS. Chưa color-normalized/training-ready, chưa có dataset chân dung.

| Phase | Phạm vi | Trạng thái / gate |
| --- | --- | --- |
| 1. Data readiness | Inventory, quyền/nhãn, chuẩn hóa kỹ thuật, split và bộ test portrait/scene/mixed | Công cụ inventory v1 đã chạy kiểm thử synthetic. Chờ thư mục ảnh thật và duyệt metadata/quyền; chưa data-ready |
| 2. Pretrained CPU baseline | Pin CanonCGT, SSL/E2E, kiểm tra loader, chạy CPU và đo output/latency/RAM | Đã chạy E2E và SSL trên 5 nhóm PST50 bằng CPU; xem [báo cáo](cpu-smoke-test-2026-10-05.md). Chất lượng chưa đạt thị giác; còn color/parity checks, RAM và benchmark ổn định |
| 3. Face/scene validation | Face boxes, landmarks, skin masks, ROI cảnh; bộ nhãn thủ công | Chưa chọn model mặt; chưa bắt đầu |
| 4. Fine-tune | Supervised và self-supervised trên data được phép; ablation vùng da/cảnh | Chưa train; gate là baseline và lỗi được xác định |
| 5. Adjust và Camera Looks | Recipe/LUT, cache, CPU parity, thumbnail cùng ảnh, lưu preset | Chưa tích hợp CanonCGT |
| 6. Pilot | Test khóa, chấp nhận AI/chỉnh tay, chất lượng vùng mặt/cảnh, máy booth | Chưa đo phần trăm |

## Phase 1 — inventory v1 đã có

Người dùng đã duyệt tải subset PST50. Đã tải cases 1–5: 15 PNG source/reference/target, revision `6ed86e34c37c558a0b2401d52f06603d619472e1`, 59,894,841 bytes ảnh. Tất cả SHA256 và byte size khớp metadata LFS upstream; inventory đọc được 15/15 ảnh 1920×1080, không có trùng byte. Lưu tại `.local/matchlook-data/pst50-starter/`, report tại `.local/matchlook-data/pst50-inventory-001/`. Dữ liệu được git-ignore.

Có upstream README, bản CC-BY-4.0, metadata nguồn, `provenance.json` và `contact-sheet.jpg` (bản xem thu nhỏ, không dùng tính metric). Đã xem 5 nhóm: núi, nội thất, núi tuyết, hồ có người, hoàng hôn; source/target nhất quán thị giác nhưng chưa đo alignment pixel. Nhóm 4 là mixed, còn lại scene. Không xem đây là tập chân dung đại diện; dataset chân dung chờ người dùng chọn.

Không ảnh nào có ICC nhúng theo decoder. Giữ `benchmark_ready=false` đến khi xác minh transfer/color pipeline Rec.709 và target/reference; không tự gán sRGB. Subset chỉ evaluation, không train; không phải full benchmark hoặc kết quả model. Phase 1 còn nhãn, color validation và test protocol. Chưa chạy CanonCGT.

Lệnh tải tái lập (cần HTTPS; destination mới, khoảng 60 MB ảnh):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/MatchLookData/Get-Pst50Starter.ps1
```

Script pin revision, giữ originals, tải license/card, kiểm tra SHA256/size từng file rồi xuất provenance/cases. Nếu tải lỗi thì dừng, thư mục dở dang không được coi là thành công; dùng destination mới khi chạy lại. Không tải video/log hoặc toàn dataset. Kết nối trong sandbox lỗi chứng chỉ; lần tải thực hiện bằng HTTPS có kiểm chứng chứng chỉ ngoài sandbox, không tắt TLS verification.

Chạy từ root repository trên Windows, dùng PowerShell và System.Drawing có sẵn. Không cần Python, GPU hoặc tải ảnh lên mạng.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/MatchLookData/Inventory.ps1 -InputDirectory 'D:\MatchLookPhotos' -OutputDirectory '.local\matchlook-data\run-001'
```

Thay đường dẫn ví dụ bằng thư mục thật. ExecutionPolicy chỉ áp dụng tiến trình lệnh này, không đổi policy máy. Output phải là thư mục mới ngoài input. Công cụ không theo junction/symlink, không sửa/xóa/move originals. Thư mục có lỗi truy cập khi liệt kê sẽ dừng thay vì tạo báo cáo đầy đủ giả. Dữ liệu và báo cáo chứa đường dẫn ảnh không được commit; dùng `.local/` đã được ignore.

Ba file kết quả:

- `manifest.json`: hash, đường dẫn tương đối, kích thước, trạng thái decode, EXIF camera/orientation/color space nếu có, ICC presence và flags. Mọi asset mặc định quarantine, quyền train false, split unassigned.
- `review.csv`: bảng để quản trị điền domain portrait/scene/mixed, capture/session, look ID, nguồn quyền và split sau khi duyệt. **Sửa CSV chưa tự cập nhật manifest**; bước importer/validator cho dữ liệu đã duyệt chưa triển khai.
- `summary.json`: số asset, số file bỏ qua, nhóm decode status. `training_ready` luôn false ở công cụ inventory; không phải bộ phê duyệt dataset.

Raster JPEG/PNG/BMP/TIFF/GIF kiểm tra frame đầu bằng decoder Windows. File trên 40 megapixel chỉ đọc header và gắn full_decode_pending để giới hạn bộ nhớ. RAW/HEIC/WebP chỉ hash/inventory, ghi decoder_required, không kết luận hỏng. Không giả định ICC present là profile hợp lệ; không chuyển màu hay sửa EXIF orientation trong phase này. Không lưu GPS/serial/danh tính người vào manifest.

Chưa có: perceptual duplicates, blur/clipping scores, ICC validation/conversion, RAW renderer, face detector/mask, tự chia split, nhập quyền từ CSV, validator ready-to-train. Vì vậy inventory v1 là phần đầu của preprocessing, không phải toàn bộ Phase 1.

## Kiểm chứng đã chạy

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/MatchLookData/Test-Inventory.ps1
```

PASS ngày 05/10/2026 trên Windows: ảnh PNG decode đúng kích thước, bản sao cùng SHA256 liên kết duplicate, JPEG hỏng bị đánh dấu, RAW không tự pass decode, không có quyền train tự cấp, hash ảnh nguồn không đổi, từ chối ghi đè report và output trong input. Fixtures synthetic được tạo ở thư mục temp riêng, không dùng ảnh khách. Chưa kiểm thử metadata camera thật, file lớn hoặc độ bao phủ mọi codec.

Tiếp theo: chạy inventory trên thư mục do người dùng chỉ định; duyệt các ảnh/nhãn và bộ test; xây bước nhập review/kiểm tra split. Các định nghĩa phần trăm và quy trình thủ công ở [workflow](implementation-and-manual-workflow.md).



## 12/10/2026 — checkpoint quality integration

See [detailed four-priority cross-check](priority-crosscheck-2026-10-12.md). RGB raw range, neutral identity and BW chroma now gate best checkpoint selection before MAE ranking. Eleven affected tests pass. Fixed-weight dry-run fails provisional criteria as expected. No CanonCGT fine-tuning, no image-quality improvement claimed. Cross-content training and explicit normalization/style separation remain incomplete.

## 12/10/2026 — normalization/style contract and procedural cross-content fixtures

[Detailed report](normalization-cross-content-2026-10-12.md): explicit linearized-sRGB EV/WB correction, per-image correction kept separate from reusable style metadata, optional adapter integration. 64 procedural cross-content fixtures with known target mapping; production readiness remains false. Seven new tests and three pipeline regression tests pass. No automatic estimator, disentangled CanonCGT style LUT, photographic cross-content training set or model quality gain claimed. No fine-tuning executed.

## 12/10/2026 — real-image known-normalization ablation

[Report](normalization-real-2026-10-12.md): 48 cases, 12 sources, 4 controlled perturbations, fixed different-session references. Known reversible correction reproduces original SSL output near numerical precision; clipped brightening worsens output consistency. Fixed numeric range tolerance (8 tests PASS). No automatic normalization enabled, no CanonCGT training or quality-improvement claim. Targets for photographic cross-content training remain pending.

### 2026-10-12 normalization robustness
96 CPU inference cases completed without training. JPEG90 exact correction blocked 10/12; JPEG70 blocked 11/12 by current strict range guard. See normalization-robustness-2026-10-12.md. Automatic normalization remains off; next is explicit bounded policy vs bypass evaluation, not tolerance relaxation.

### 2026-10-12 bounded JPEG policy
Experimental CorrectionPolicy with explicit bypass implemented, not enabled in WPF/default adapter. 96 paired CPU cases, 15 tests passed; JPEG90 fallback 9/12, JPEG70 10/12. See correction-policy-2026-10-12.md. User-confirmed manual capture + continuous light/flash recorded in controlled-booth-capture. No training or hardware control.

### 2026-10-12 clipped-source negative controls
48 cases complete in38.153s; correction-range guard missed all known simulated source-loss cases. Optional verified-source-loss bypass added, not auto detection or app default.18 tests PASS. See clipping-policy-2026-10-12.md and booth-calibration-capture-checklist.md. No fine-tune or hardware activity.

### 2026-10-12 controlled capture intake
Read-only ControlledCaptureIntake validator and camera/capture templates implemented.10 tests PASS. Empty template honestly reports0captures/notready; RAW decode, color calibration and real hardware not executed. See controlled-capture-intake-2026-10-12.md. Calibration/validation protocol does not change training85/15 split. No fine-tune.

### 2026-10-12 CustomerImages additions
192JPEG checked against prior176hashes;16new files, all decode; previous176unchanged, no exact duplicates. Visual contact sheet inspected. Source/edit/session roles pending; no RAW/liveview/chart provided in this folder. No split updates or training. See customer-additions-2026-10-12.md.

### 2026-10-12 user confirmed new source roles
User confirms all16new JPEGs are straight from camera without post-capture color editing, and authorizes training use. Rechecked16hashes and wrote local source-manifest.confirmed.json. Group/split/target readiness remains pending; previous85/15 unchanged. No fine-tune. Camera-rendered JPEG is not assumed neutral RAW.

### 2026-10-12 two-step fine-tune preparation COMPLETE (development scope)
Active pack finetune-prep-2026-10-12-002:94train/48validation/30test/20historical-test quarantine.16newJPEG all train.2256train+384validation examples;139/142safe predicted skin masks. Full2640sample preflight and24gradient cases PASS in81.712s;3extra new-image backward checks PASS.30tests PASS. Zero CanonCGT optimizer steps, unchanged state hash. Official SSL baseline fails182case-metric quality checks. Config execution remains disabled pending prior-notification step; production readiness not claimed. Pack001 superseded/disabled. See finetune-preparation-2026-10-12.md.

### UI lựa chọn thông số — 23/10/2026

Thêm checkbox con Light & Contrast (Contrast/HDR/RGB Curve), Color Adjust (Saturation và ba trục HSL), Color Tone (ba vùng Balance và ba RGB channel curves). MatchComponents lọc tham số optimizer; mặc định All giữ nguyên fit cũ. Không đổi model, loss hay heuristic. Kiểm chứng: 192 tests, 4 simulator scenarios, WPF offscreen kiểm tra 16 checkbox. Không kiểm tra màn hình thật theo yêu cầu tránh chuyển focus. Bản thử: .local/builds/adjust-multicolor-2026-10-23/.