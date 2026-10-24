# Phase A — báo cáo kiểm chứng CPU, 07/10/2026

## Kết luận

Pipeline CPU SSL tái tạo gần như trùng 8 bundled sample outputs của upstream. Không phát hiện lỗi cấu hình mạng, loader RGB, trục LUT hoặc bỏ sót bước canonicalization/restyling trong phạm vi kiểm thử. Model có phản ứng với reference nhưng không chuyển đầy đủ monochrome trong 3 probe ảnh booth. Chuyển màu yếu không thể quy ngay cho adapter: hiện đã tái lập hành vi upstream trên ảnh mẫu.

Technical baseline đạt các kiểm tra dưới đây. Quality gate chưa đạt, chưa có bằng chứng >90% hoặc đủ điều kiện sản phẩm. Không fine-tune hoặc sửa output để làm đẹp kết quả trong lượt này. Phase B có thể xây benchmark thật dựa trên baseline này; không mở train lớn chỉ từ kết quả audit.

## Phạm vi và tái lập

Tool `tools/MatchLookData/Audit-CanonCgtPhaseA.py`; output `.local/matchlook-data/phase-a-audit-001/` gồm report.json, index.html, PNG từng bước, official-comparison.jpg và customer-tone-probes.jpg. Hai ảnh tổng hợp được tạo để review từ PNG kết quả. Ảnh khách không đưa vào Git hoặc gửi dịch vụ ngoài.

```powershell
.local/canoncgt-env/Scripts/python.exe tools/MatchLookData/Audit-CanonCgtPhaseA.py --repo .local/CanonCGT --weights .local/canoncgt-official-weights --customer D:/MatchLook/CustomerImages --output .local/matchlook-data/phase-a-audit-NEW
```

CPU FP32, 4 threads; commit/hash checkpoint và file mạng/cấu hình/demo ghi trong JSON. Kiểm tra network config khớp YAML upstream. Strict-load tất cả inference tensors; E2E bỏ riêng style_centroids dùng cho training. Không tải ImageNet initialization vì weights được checkpoint ghi đè đầy đủ.

Chạy 8 official sample pairs cho mỗi checkpoint, tổng 16 cases. Chọn 3 ảnh khách bằng vị trí đầu/giữa/cuối theo tên file, không coi là 3 phiên/người độc lập. Mỗi source dùng chung một reference ở bốn biến thể neutral/warm/cool/monochrome, tổng 24 probes; thêm self-reference và lặp cùng input. Các biến thể là phép thử tổng hợp, không phải target được quản trị duyệt. “Neutral” chỉ nghĩa reference chưa thêm biến đổi thử nghiệm, không khẳng định JPEG chưa chỉnh.

## Số đo

| Kiểm tra | Kết quả |
| --- | --- |
| Identity LUT trên RGB ngẫu nhiên và điểm biên | Max error 1,19e-7 trong miền 0–1; assertion <1e-6 PASS |
| Loader numpy so ToTensor theo demo, CPU | Assertion max error <1e-6 PASS trên 16 cases |
| Tái áp hai LUT tuần tự so forward | Assertion max error <1e-6 PASS trên 16 cases |
| Lặp cùng input/reference | Assertion max error <1e-6 PASS trên 6 cặp source/checkpoint |
| SSL so 8 bundled outputs sau lượng tử PNG | MAE khoảng 0,00001–0,00008 trên thang kênh 0–255 |
| E2E so bundled outputs | MAE 4,24–27,37; không coi bundled SSL-like outputs là ground truth để kết luận E2E sai kỹ thuật |
| SSL đổi warm/cool reference so neutral | Output MAE khoảng 3,98–18,60 trên 3 source; chứng minh nhạy reference, không phải điểm chất lượng |
| Monochrome reference | Reference channel spread = 0; SSL output spread 0,0742–0,0888, E2E 0,0845–0,1109; vẫn còn màu |
| Out-of-range trước clamp, max qua cases/probes | SSL khoảng 1,95% giá trị kênh; E2E khoảng 0,132%. Đây không phải tỷ lệ pixel clipping sau xuất |

Channel spread = trung bình max(R,G,B)-min(R,G,B), dùng đo còn sắc màu, không phải skin metric hoặc perceptual similarity. MAE không được đổi thành % giống. So bundled examples có giá trị tái lập, không chứng minh tác giả đã dùng đúng cùng checkpoint vì không có provenance runtime đầy đủ cho ảnh bundled.

Xem trực quan official case 01: SSL sát bundled, E2E tạo vùng trời vàng mạnh. Probe booth: SSL đổi sáng/màu giữa references nhưng vẫn còn màu ở reference đen trắng, và tone lạnh chưa tái hiện đầy đủ. Không suy rộng từ 3 ảnh ra toàn bộ dataset.

## Color pipeline và giới hạn

Demo dùng PIL RGB -> ToTensor [0,1], không ImageNet normalization, không linearize và không ICC transform. Audit official giữ đúng quy ước đó. Serializer upstream đổi RGB sang BGR trước cv2; PIL ghi RGB trong audit là tương đương về thứ tự kênh.

Ảnh khách được EXIF-orient và giảm cạnh dài <=960, không tự chuyển ICC. Inventory ghi EXIF sRGB cho batch; chưa xác minh mọi profile/picture style. Đối với sản phẩm: đọc EXIF trước, chuyển ICC hợp lệ sang không gian đầu vào thống nhất, ghi provenance khi thiếu profile, không normalize exposure/WB ẩn vì có thể xóa look. Chưa triển khai color-managed loader trong audit. Rec.709 của PST50 vẫn là câu hỏi riêng, chưa được audit này giải quyết.

Đối chiếu demo ở đây là CPU-equivalent loader + upstream model, không phải chạy nguyên script CUDA/PyTorch 1.12.1. Chưa đo CPU/GPU parity hoặc parity ONNX. Chưa có mask mặt/da hay target người duyệt, chưa đo chất lượng da. Latency trong JSON chỉ forward một lần/case, không thay benchmark lặp hoặc NUC thật.

## Quyết định tiếp tục

1. Giữ SSL làm mốc tái lập chính, giữ E2E làm đối chứng; không tuyên bố SSL luôn đẹp hơn.
2. Đưa monochrome, cool-tone fidelity và highlight overshoot vào regression cases của Phase B.
3. Tạo source–reference–target được duyệt và look labels, kiểm chứng normalization riêng; không ép mọi tone về da trung tính.
4. Chỉ fine-tune sau khi benchmark và nhóm dữ liệu được khóa. ONNX phải giữ parity với baseline này; tối ưu CPU không giải quyết được giới hạn màu đã quan sát.
