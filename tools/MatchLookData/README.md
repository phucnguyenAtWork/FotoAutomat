# MatchLookData — chỉ mục script

Script Python/PowerShell phục vụ dữ liệu cho Match Look; không thuộc build .NET. Chạy từ root repo. Mỗi `Test-*.py` là unit test của script cùng tên.

Từ 14/10/2026, Match Look đi theo hướng **thông số trước** ([định hướng](../../docs/ai-match-look/match-look-direction-2026-10-14.md)). Cùng ngày, theo yêu cầu người dùng, đã **xóa 71 script nghiên cứu CanonCGT/LUT** cùng test của chúng, và thư mục `tools/LangSmithJudge`. Các script đó gồm: chạy và fine-tune CanonCGT, loss/gradient, chuẩn hóa đầu vào, hậu xử lý output, self-reference correction, limiter, bảo vệ da kiểu LUT, LUT fitting thử nghiệm.
- Bản sao đầy đủ, đã đối chiếu hash: `.local/archive/matchlook-canoncgt-research-2026-10-14.zip`.
- Danh sách file: `.local/archive/delete-list-2026-10-14.txt`.
- Lệnh tái hiện trong các báo cáo cũ ở `docs/ai-match-look/` cần giải nén zip này về đúng đường dẫn.

## Script còn lại — dùng cho hướng thông số

| Nhóm | Script |
|---|---|
| Kiểm kê, tải, chọn dữ liệu | `Inventory.ps1`, `Get-Pst50Starter.ps1`, `Select-OpenImages.py`, `Download-OpenImages.py`, `Preprocess-Review.py`, `Rank-DuplicateCandidates.py`, `Create-RequestedSplit.py`, `Prepare-CustomerPilot.py` |
| Look tổng hợp (nguồn dữ liệu train cho bộ dự đoán thông số) | `PhaseBData.py`, `Build-PhaseB.py`, `Validate-PhaseB.py` |
| Mask da / vùng mặt (đánh giá phương án A) | `Prepare-FinetuneMasks.py`, `RegionQuality.py`, `Audit-PortraitRegions.py`, `Audit-RegionStability.py` |
| Ảnh chụp booth có kiểm soát | `ControlledCaptureIntake.py` |
| Duyệt dữ liệu | `TrainingReview.py`, `Build-TrainingReview.py` |
| Dataset bộ dự đoán thông số (P0, 16/10) | `Prepare-ParamContent.py` (ảnh nội dung 256 px BGRA + manifest); nhãn recipe sinh bằng tool .NET `tools/FotoAutomat.MatchDataset` |
| Bộ render khả vi cho train (P1, 16/10) | `ColorRenderTorch.py`: bản PyTorch của `ColorProcessor` cho 52 thông số, **chỉ dùng khi train**; C# là nguồn chuẩn |
| Bộ dự đoán thông số (P2, 16/10) | `ParamPredictor.py` (model, lấy mẫu, loss), `Train-ParamPredictor.py`, `Evaluate-ParamPredictor.py` (tập test cố định + so với v4 qua lệnh `v4` của tool .NET) |
| Bộ dự đoán thông số P3 (17/10) | `look-library-v1.json` (30 look chân dung), `look-splits-p3.json`, `Render-LookLibrary.py` (bảng duyệt look + chỉ số da), `ParamPredictorP3.py` (Encoder T, guardrail), `Train-ParamPredictorP3.py` (chỉ chạy khi người dùng duyệt), `Evaluate-ParamPredictorP3.py` (tập test 300 mẫu, ridge, v4 K reference), `Check-P3Feasibility.py` (ridge trước khi train, `--lolo` bỏ-một-look) |

## Test (đều đạt sau khi xóa, 14/10/2026)

- `Test-OpenImages.py`, `Test-Preprocess.py`, `Test-PrepareParamContent.py`, `Test-ColorRenderParity.py` (tham số: thư mục param-dataset; cổng parity với C#), `Test-ParamPredictor.py`, `Test-ParamPredictorP3.py`, `Test-PhaseB.py`, `Test-ControlledCaptureIntake.py`, `Test-TrainingReview.py`: chạy bằng `.local\canoncgt-env\Scripts\python.exe`.
- `Test-PortraitRegions.py`, `Test-RegionQuality.py`: cần `cv2`/MediaPipe, chạy bằng `.local\region-audit-env\Scripts\python.exe`.
- `Test-Inventory.ps1`: chạy bằng PowerShell.
