# Kết quả correction chuẩn hóa exposure — 13/10/2026

Đã thực thi [protocol đăng ký trước](exposure-normalized-correction-2026-10-13.md). 128 ca dev, 36,24 giây CPU, không optimizer. Ba nhánh đối chứng joint_mapped/fixed_a10/reest_a10 khớp từng metric của lượt 2, sai số tối đa 0. State model và hash đầu vào không đổi; không đọc test.

## Kết quả

| Nhánh | Macro | Neutral | Low-key | Da | Highlight | Tốt / tệ | Lỗi mapped / raw | Đạt tiêu chí dev |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Joint mapped | 7,545 | 7,234 | 4,430 | 7,991 | 13,850 | — | 16 / 74 | Mốc |
| Norm α=0,25 | 6,895 | 6,158 | 3,957 | 7,459 | 11,283 | 103 / 25 | 16 / 69 | Không: neutral chưa giảm đủ |
| Norm α=0,5 | 6,451 | 5,239 | 3,898 | 7,256 | 9,020 | 97 / 31 | 15 / 96 | Có |
| Norm α=0,75 | 6,260 | 4,668 | 4,251 | 7,479 | 7,314 | 90 / 38 | 15 / 146 | Có |
| Norm α=1 | 6,371 | 4,528 | 4,831 | 8,038 | 6,838 | 82 / 46 | 13 / 181 | Có; ứng viên theo quy tắc đã đăng ký |

Đơn vị MAE /255. Mỗi số tổng hợp giữ cùng tập ca; da 128, highlight 112. Lỗi là ca–tiêu chí, không phải số ảnh. Đạt tiêu chí dev gồm neutral giảm ≥1,5, mỗi look không tăng quá1,0, macro không tăng, da không tăng quá0,5; **không đồng nghĩa đạt quality gate**.

α=1 được chọn đúng quy tắc ưu tiên neutral thấp nhất trong các nhánh đạt. Không thay quy tắc sau khi xem kết quả để chọn α=0,75 (macro thấp nhất) hoặc α=0,5 (da tốt hơn, ít raw violations hơn). Giữ các nhánh này để mô tả đánh đổi, chưa gọi nhánh nào đủ production.

## Ý nghĩa và giới hạn

Chuẩn hóa self-output về mức sáng source trước khi lấy residual giúp giảm hiện tượng bù trùng độ sáng. Cả ba α≥0,5 nay qua tiêu chí dev, trong khi correction chưa chuẩn hóa của lượt 2 không nhánh nào đạt. Đây là bằng chứng có ích cho phương pháp trên tập đã thử, chưa là kết luận về cơ chế nội bộ hoặc khả năng tổng quát.

8/8 source có normalization hợp lệ, không bypass; median luminance sai số tối đa 5,96e-8. Gain self khoảng −0,18 đến −0,48 EV. Không lấy target để ước lượng gain/residual. Nhánh correction vẫn phụ thuộc ảnh nguồn, không phải preset cố định dùng chung.

Nhánh α=1: neutral giảm 2,706; low-key tăng 0,401 (trong giới hạn1,0); da tăng0,047 (trong giới hạn0,5); highlight giảm7,012. Tuy nhiên 46/128 ca tệ hơn mốc. Trung bình tốt lên không đảm bảo mọi khách đều tốt hơn.

Raw violations tăng 74→181: 78 lỗi tỷ lệ RGB, 89 excursion, 14 neutral. Sau gamut mapping còn13 neutral; mapping xử lý miền màu chứ không làm model hết lỗi. Gate neutral3/255 vẫn chưa đạt. **Không tích hợp hoặc train tiếp trong lượt này.**

Ưu tiên nghiên cứu tiếp: kiểm tra vị trí và mức excursion trước mapping, tìm cách giới hạn correction theo headroom RGB mà không đổi mục tiêu phong cách; đăng ký đối chứng mới trước khi chạy. Nếu dùng α=1 như ứng viên, giữ nguyên để kiểm chứng độc lập sau đó, không liên tục chọn theo cùng tập validation. Không mở30test để chọn phương pháp. Bằng chứng trên dev lặp lại chưa thay thế ảnh booth mới cùng cấu hình.

## Code, kiểm thử và tái hiện

- `Run-ExposureNormalizedCorrection.py`: chuẩn hóa sRGB tuyến tính, residual cap, fixed baseline gain, cache self-reference, parity và quyết định dev.
- `Test-ExposureNormalizedCorrection.py`:3 tests qua — pure gain triệt residual, identity/black/extreme gain, cap khi có excursion. Đã xem ảnh kết quả đại diện; chưa duyệt visual tất cả128ca.
- Output: `.local/matchlook-data/exposure-normalized-correction-2026-10-13-001/` có protocol snapshot/hash, summary/cases/raw-cases/ev/plan JSON và HTML/PNG. Protocol ban đầu giữ nguyên, kết quả ghi riêng tại tài liệu này.

```powershell
.\.local\canoncgt-env\Scripts\python.exe tools\MatchLookData\Test-ExposureNormalizedCorrection.py
.\.local\canoncgt-env\Scripts\python.exe -u tools\MatchLookData\Run-ExposureNormalizedCorrection.py --root .local\matchlook-data\finetune-prep-2026-10-12-002 --checkpoint .local\matchlook-data\finetune-2026-10-12-001\last.pth --previous .local\matchlook-data\self-reference-correction-2026-10-13-002 --protocol docs\ai-match-look\exposure-normalized-correction-2026-10-13.md --output .local\matchlook-data\exposure-normalized-correction-2026-10-13-001
```

Chọn output mới nếu chạy lại. Không có holdout mới, không fine-tune, không model/preset mới trong WPF.
