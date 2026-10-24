# Tiếp nhận dữ liệu kiểm chuẩn booth — 12/10/2026

## Đã triển khai

`tools/MatchLookData/ControlledCaptureIntake.py` là CLI chỉ đọc ảnh và ghi báo cáo mới. Không điều khiển camera/đèn, không sửa ảnh nguồn, không giải mã RAW, không train, không tạo profile hay LUT. Không dùng dữ liệu synthetic làm ảnh chụp thật.

Công cụ kiểm tra:

- Cấu hình camera/đèn: model/lens/firmware, ISO/shutter/aperture, M/Auto ISO tắt, fixed/custom WB, JPEG style/color space, flash manual/power, đèn liên tục và vị trí setup.
- Capture ID/config, session/burst, nhóm calibration/validation, subject chart/one_person/four_people, vị trí, khai báo quyền sử dụng và xác nhận ghép đúng RAW–JPEG–liveview.
- File nằm trong root được chỉ định; SHA256 khớp manifest; JPEG/liveview decode được. RAW chỉ kiểm tra extension và hash: file có đuôi RAW không chứng minh decoder đọc được.
- Cùng session/burst hoặc file hash không được ở hai partition. Chưa kiểm tra near-duplicate hoặc tính đúng của khai báo thông số/pairing trên phần cứng.
- Độ đủ của đợt thu dữ liệu, độc lập với quyền train hay chất lượng màu. `training_ready` và `profile_ready` luôn false.

## Số ảnh tối thiểu của template

Mỗi cấu hình camera, mỗi lượt:

| Nội dung | Số capture |
| --- | ---: |
| Bảng màu ở tâm/trái/phải, 3 lần mỗi vị trí | 9 |
| Một người ở vùng đứng, 3 lần | 3 |
| Nhóm 4 người bố trí tại các dấu đứng, 3 lần | 3 |
| Tổng | 15 |

Một lượt calibration và một lượt validation ở session khác: **30 capture**, mỗi capture gồm RAW+JPEG+frame liveview tương ứng. Đây là protocol thử nghiệm ban đầu, chưa phải số lượng đủ train. Bảng màu có thể đặt tại các vị trí rìa còn nhóm người đứng theo setup; không yêu cầu nhóm 4 người dịch cả nhóm ra từng vị trí bảng màu.

Hai partition trên dành riêng kiểm chuẩn, KHÔNG sửa tỷ lệ train/test85/15 đã yêu cầu cho dataset training. Ảnh reference look đặt riêng, không coi reference là RAW/JPEG ground truth của booth.

## Cách dùng

1. Copy `controlled-capture-manifest.template.json` ra thư mục local cho cấu hình thực tế. Các giá trị null phải điền theo setup thật, không lấy các con số trong test.
2. Với mỗi capture, thêm record theo `controlled-capture-record.example.json` vào `captures`; ghi đúng relative path và SHA256. Trên PowerShell có thể dùng `Get-FileHash -Algorithm SHA256 -LiteralPath 'duong-dan-anh'`; chuẩn hóa hash thành chữ thường để khớp giá trị công cụ tính.
3. Chỉ đánh dấu pairing_verified/rights_confirmed sau khi thực sự xác nhận. Nếu camera chưa xuất được liveview, để thiếu và báo cáo sẽ ghi blocker; không lấy JPEG cuối giả làm liveview.
4. Chạy từ repo với Python môi trường hiện có:

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/ControlledCaptureIntake.py .local/matchlook-data/controlled-capture-001/manifest.json --root .local/matchlook-data/controlled-capture-001 --report .local/matchlook-data/controlled-capture-001/intake-report-002.json
```

Exit0 khi intake đủ, exit2 khi chưa đủ; JSON phân biệt errors và blockers. Không ghi đè report cũ. Không tự ghi SHA vào manifest để vô tình xác nhận file đã bị đổi; SHA cần lập lúc tiếp nhận dữ liệu nguồn.

## Đã kiểm chứng lượt này

10 tests PASS: empty/complete intake, hash change, root escape, pairing pending, session overlap, duplicate file across partitions, corrupted JPEG, Auto ISO, RAW decode/profile status. Test có dummy RAW và ảnh synthetic, chỉ kiểm tra validator; không kết luận RAW camera dùng được.

Chạy template chưa điền: cấu trúc hợp lệ nhưng capture_count0, intake_completefalse, training_readyfalse, profile_readyfalse. Report local `controlled-capture-001/intake-report.json`. Không có calibration dataset thật được khai báo trong lượt này; không đánh dấu các JPEG đã có preset thành dữ liệu kiểm chuẩn.

## Bước tiếp theo phụ thuộc dữ liệu thật

Khi có capture manifest/file: kiểm tra intake, xác minh RAW decode/ICC/EXIF, pairing, vùng chart và chất lượng ảnh; đo lặp lại và chênh liveview/capture. Sau đó mới thử transform riêng cho hai luồng. Mọi thử nghiệm fine-tune phải thông báo người dùng trước; tool này không nối vào TrainingPipeline và không cấp training authorization.
