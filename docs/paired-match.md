# Paired Match v1 — 04/10/2026

Đã triển khai baseline chạy cục bộ bằng tối ưu tham số, **chưa phải AI và chưa đạt nghiệm thu trên 90% màu**. Engine `PairedMatchEngine` nay chỉ giữ trong Core/benchmark nội bộ. Đã bỏ Paired/Match method khỏi GUI vì không phù hợp luồng người dùng; GUI chỉ chạy [Reference Match](reference-match.md), nhận ảnh khác cảnh/kích thước/tỷ lệ, chọn tone mong muốn trước rồi ảnh cần chỉnh. Các bước Paired dưới đây ghi lại luồng v1 trước khi bỏ khỏi UI, không còn là hướng dẫn GUI hiện tại.

## Luồng sử dụng

Hai vùng ảnh lớn cạnh nhau: chọn reference bên trái trước rồi ảnh cần chỉnh bên phải; bên phải chuyển Before/After. Vùng trống bấm được để chọn ảnh. Các nhóm tìm thông số, nhãn camera và số đo/Export nằm trong **Nâng cao**, mặc định thu gọn. Riêng Paired Match chỉ bật khi đủ hai ảnh và xác nhận khớp vị trí; After/thử ảnh khác/chuyển Adjust chỉ bật sau khi có kết quả.

1. Chọn **Source** là ảnh gốc và **Reference** là ảnh có tone của **cùng cảnh, cùng crop và đã khớp vị trí**. Xác nhận checkbox rồi chạy Match. Bản này không tự căn chỉnh ảnh, không nhận ảnh khác cảnh làm cặp.
2. Chọn các nhóm Normalize, Light/Tone Curves và Color/HSL/RGB Curves. Có tiến độ và Cancel. Đổi ảnh hoặc nhóm tìm sẽ bỏ kết quả cũ; không cho chuyển kết quả đã hủy sang Adjust.
3. So Source/Matched; chọn **Ảnh kiểm tra khác** để áp đúng công thức đã tìm lên ảnh chưa fit. Ảnh kiểm tra không thay đổi công thức, nhưng chưa có ảnh đích để tự chấm điểm trong UI.
4. Nhập tên preset, camera booth và camera/tone tham khảo nếu biết; không biết giữ Unknown. **Giữ Exposure/WB** mặc định bật để preview đúng công thức vừa tìm. Có thể bỏ chọn để loại bù sáng/WB khỏi preset; preview đổi theo lựa chọn này. Các số Fit/Check luôn ghi kết quả Match đầy đủ ban đầu, không phải điểm của công thức đã bỏ Normalize.
5. **Chỉnh tiếp trong Adjust →** thêm và chọn bản nháp, mở tab Adjust để sửa thông số. Chưa tự lưu, đổi preset mặc định hoặc phát hành bộ lọc cho khách. Sau khi đánh giá nhiều ảnh, bấm **Lưu preset** trong Adjust.
6. **Export benchmark JSON** ghi công thức đầy đủ, số đo, lựa chọn giữ Normalize và nhãn camera vào tệp do quản trị chọn. Không chứa pixel ảnh. Origin trong preset là thông tin quản trị nhập và số check ban đầu; không phải EXIF/setting máy ảnh đã được engine khôi phục.

## Engine hiện tại

Input Core: 1–16 cặp BGRA8 cùng kích thước; UI v1 chọn một cặp. Lấy mẫu không gian xác định, mặc định 2048 pixel tổng; dành khoảng 20% tọa độ riêng để kiểm tra. Chỉ dùng pixel alpha 255. Ít pixel hợp lệ thì báo lỗi. Không sửa source/reference.

Tối ưu coordinate descent từ công thức trung tính, nhiều mức bước giảm dần và số lần thử hữu hạn. Mọi candidate chạy qua `ColorProcessor` của ứng dụng. Nhóm tham số gồm Exposure/Warmth/Tint, Contrast/Highlights/Shadows/Whites/Blacks/Saturation, ba điểm giữa Curves RGB và từng kênh, HSL tám nhóm. Curve được giữ đơn điệu. Có ưu tiên rất nhỏ cho công thức đơn giản khi sai khác gần bằng nhau.

Chưa fit Color Grading wheel, Levels, LUT, Calibration, B&W hoặc semantic mask. Không học Retouch, Sharpening, Grain hay hiệu ứng phụ thuộc vị trí. Không có model học máy, training weights hoặc reverse-engineering Capture One. Không đảm bảo tìm được nghiệm tốt nhất toàn cục hay đúng thông số gốc.

## Số đo và benchmark

**RGB RMSE** là căn trung bình bình phương sai khác ba kênh sRGB, đơn vị mức kênh 0–255; thấp hơn tốt hơn. Before/Fit dùng tập tọa độ fit; Check dùng tọa độ chưa fit trong cùng cặp. Không chuyển số này thành phần trăm độ giống hoặc Delta E. Check trong cùng cảnh chưa chứng minh khả năng dùng lại nhiều camera/điều kiện sáng.

Chạy benchmark portable có thể lặp lại:

```powershell
dotnet run --project tools/FotoAutomat.MatchBenchmark
```

Ba tone tổng hợp có công thức biết trước; fit hai ảnh, đo thêm một ảnh khác chưa dùng để fit. Output mỗi dòng là JSON kèm recipe và số đo. Ngưỡng pass là regression test baseline, không phải định nghĩa mục tiêu sản phẩm 90%. Benchmark này không thay thế ảnh booth thực tế hoặc dữ liệu máy ảnh của người dùng.

WPF đã thử cặp ảnh dẫn xuất từ ảnh khách bằng công thức kiểm soát: Before 25,832, Fit 0,609, Check 0,628; số này không đại diện màu camera thật. Kiểm tra chuyển bản nháp/chọn Adjust/lưu preset, preview ảnh khác, hủy, bỏ kết quả cũ và EXIF rotation đã pass. Chưa có cặp camera thực tế để báo độ chính xác thương mại.

Kiểm tra hoàn tất: 116 unit tests, ba ca benchmark tổng hợp, WPF Match/Retouch/Refine và bốn kịch bản harness phiên chụp đều pass. Debug/Release build thành công; Release không có cảnh báo. Benchmark warm tone trên ảnh chưa fit giảm RGB RMSE từ 34,93 xuống 2,05; đây vẫn là dữ liệu tổng hợp do ColorProcessor tạo.

## Chuẩn hóa ảnh và giới hạn

Match và phòng màu dùng chung `MatchImageReader`: tệp tối đa 40 MB, ảnh tối đa 40 triệu pixel gốc, preview cạnh dài tối đa 960 px, xử lý EXIF orientation. Profile nhúng được chuyển sang sRGB bằng WPF ColorConvertedBitmap; ảnh không profile được **giả định sRGB**, không tự suy đoán Adobe RGB. Nếu profile không chuyển được sẽ báo lỗi. Tham khảo [Microsoft ColorContext](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.colorcontext?view=windowsdesktop-10.0).

Ảnh được decode bởi WIC trước khi resize; giới hạn kích thước không phải cam kết bộ nhớ đỉnh thấp trên mọi tệp. JPEG/PNG/BMP/TIFF, chưa RAW/HDR hoặc xuất full resolution. Preset lưu theo máy qua thư viện hiện có; kết quả Match riêng chưa được lưu workspace qua lần đóng tab.

Adjust vẫn áp Retouch/Refine đã phát hành cho cùng ảnh theo pipeline hiện có; nếu đang có các hiệu ứng này thì preview Adjust có thể khác ảnh Match thuần màu. Không tự reset các hiệu ứng đã lưu của người dùng.

Tiếp theo: dữ liệu paired thực tế, benchmark theo cảnh/camera chưa fit, color management đầu vào kiểm chứng, Color Grading/semantic regions và đối chiếu model AI theo [thiết kế](match-look-design.md). Reference Match thống kê khác cảnh đã có baseline riêng, chưa phải semantic/AI.
