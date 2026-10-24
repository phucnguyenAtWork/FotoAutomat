# Đối chứng chính sách đầu ra — 13/10/2026

## Phạm vi và kết quả

Đã chạy 128 ca validation giống exposure-monochrome-2026-10-13-002, 36,32 giây CPU, 0 optimizer steps. State model và checkpoint/config/manifest không đổi. Baseline và joint tái hiện chính xác toàn bộ metric từng ca của lần trước. Không dùng test, không tích hợp WPF hoặc xuất preset production.

| Nhánh | MAE /255 | Da | Highlight | Low-key | Neutral | Vi phạm ca–tiêu chí |
|---|---:|---:|---:|---:|---:|---:|
| LAST | 17,600 | 18,459 | 12,895 | 46,079 | 15,055 | 134 |
| Exposure + BW (joint) | 7,542 | 8,000 | 13,810 | 4,430 | 7,234 | 74 |
| Half EV + BW | 10,744 | 11,795 | 11,005 | 20,802 | 9,266 | 84 |
| Highlight protected + BW | 12,771 | 11,612 | 12,378 | 18,256 | 13,263 | 117 |
| Joint + gamut mapping | 7,545 | 7,991 | 13,850 | 4,430 | 7,234 | 16 |
| Protected + gamut mapping | 12,811 | 11,632 | 12,554 | 18,256 | 13,342 | 16 |
| Identity control | 17,279 | 18,059 | 9,723 | 72,168 | 0 | 16 |

Da có 128 ca, highlight 112 ca trong mọi nhánh. Không nhầm 16 vi phạm còn lại giữa các nhánh: mapped còn 16 neutral; identity còn 16 monochrome. Không có nhánh nào đạt gate.

## Thuật toán đã triển khai

`tools/MatchLookData/Benchmark-OutputPolicy.py` tái sử dụng adapter, loader, gain/BW detector và metric hiện có. Các ngưỡng đặt trước lượt chạy; không dùng target hoặc nhãn look để chọn policy.

1. **Half EV:** EV giảm một nửa, tương đương gain mới là căn bậc hai gain cũ. Đo xem giảm cường độ toàn ảnh có bảo vệ highlight với đánh đổi chấp nhận được không.
2. **Highlight protected:** tính luminance trên output encoded RGB; từ 0,6 đến 1,0 dùng smoothstep để giảm dần tác động Exposure. Gain hiệu dụng là `1 + (gain-1)*weight`. Áp gain trong linear light rồi BW nếu detector bật. Giữ vùng sáng gốc của model không đảm bảo giữ đúng phong cách reference; low-key có thể muốn làm tối chính vùng này.
3. **Gamut mapping:** trên encoded RGB, lấy luminance làm trục xám; đưa luminance ngoài [0,1] về biên; giảm độ dài vector chroma bằng một hệ số chung đến khi ba kênh nằm trong RGB cube. Cuối cùng clamp sai số số học. Giữ hướng chroma trong hệ RGB này, không tuyên bố bảo toàn hue cảm nhận hoặc xử lý ICC/HDR.
4. **Identity control:** trả source cho tất cả look, chỉ để xác minh metric và biết mức lỗi khi bỏ qua model. Không dùng nhãn neutral để chọn bypass — làm vậy sẽ dùng thông tin oracle không có trong sản phẩm.

Mapping bảo đảm miền đầu ra bằng cấu trúc; **không chứng minh model hết lỗi gamut**. Báo cáo giữ joint/protected trước mapping và `mapping.json` với thay đổi từng ca. Joint trước mapping vẫn có 26 lỗi tỷ lệ RGB và 32 excursion. Sau mapping, 58 lỗi này biến mất do phép biến đổi, còn 16 neutral.

Joint mapping thay đổi trung bình 1,781% pixel mỗi ca; độ thay đổi tuyệt đối trung bình 0,054/255, nhưng cực đại 24,929/255. Trung bình nhỏ không đảm bảo mọi vùng ảnh đều không thấy khác. MAE da giảm rất nhẹ, highlight tăng nhẹ.

## Kết luận và quyết định

- Gamut mapping là ứng viên cho bước xuất đầu ra hợp lệ; giữ riêng lỗi raw trước mapping trong tiêu chí đánh giá model.
- Không chọn Half EV hoặc highlight protection làm mặc định: highlight tốt hơn nhưng low-key tăng từ 4,43 lên 20,80 hoặc 18,26. Không có cải thiện đồng thời trên các mục tiêu chính.
- Neutral còn lỗi thật về độ giống target: joint/mapped khoảng 7,23/255, vượt gate 3/255. Identity control cho neutral bằng 0, xác nhận target neutral thực sự là source; không phải lỗi công thức metric.
- Không suy ra từ đó rằng hệ thống có thể nhận diện một reference bất kỳ là neutral. Một ảnh xám, sáng tự nhiên hoặc màu trung tính không tự chứng minh preset identity.
- Chưa chạy bước đóng băng cả LUT vì policy chưa đạt điều kiện chất lượng đã đặt. Không fine-tune thêm chỉ để thử che lỗi này.

Ưu tiên tiếp theo: chẩn đoán neutral theo cặp source/reference cùng preset nhưng khác nội dung, đo thay đổi theo vùng và conditioning; đánh giá lại ước lượng độ sáng có xét độ tin cậy/nội dung thay vì bảo vệ highlight mù. Sau đó kiểm tra fixed LUT trên bộ ảnh cùng cấu hình booth. Dữ liệu hiện là look tổng hợp tăng thêm trên JPEG đã render; chưa phải benchmark booth hiệu chuẩn, blind test hoặc full-resolution.

## Kiểm thử và chạy lại

Ba kiểm thử đã qua: mapping giữ ảnh trong miền và đưa excursion vào miền; giữ luminance khi luminance gốc hợp lệ và thứ tự kênh trên ca thử; highlight smoothstep đạt đúng endpoint. Đã xem ảnh output đại diện; chưa duyệt visual toàn bộ 128 ca.

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-OutputPolicy.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Benchmark-OutputPolicy.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --experiment .local\matchlook-data\loss-ablation-2026-10-12-001\experiment.json --output .local\matchlook-data\output-policy-2026-10-13-001
```

Chọn output mới khi chạy lại. Artifact có summary/cases/mapping/plan JSON và HTML/PNG đối chiếu. Script ghi hash, ngưỡng và giới hạn; không thay checkpoint hoặc gate gốc.
