# Adjust — cập nhật 21/10/2026

## Đã triển khai

- White Balance: Shot/Custom; chỉnh Kelvin hoặc Tint tự chuyển Custom. Kelvin 800–140000, mốc 5000; Tint −50…50. Shot giữ cân bằng trắng đã có trong JPEG.
- Exposure: −4…4, hiển thị hai chữ số thập phân; Contrast/Brightness −50…50; Saturation −100…100.
- High Dynamic Range: Highlight/Shadow/White/Black −100…100.
- Levels giữ hai output handle phía trên, ba input handle phía dưới, số nhập trực tiếp và chọn kênh. Đã đối chiếu video 8401167426561.mp4. Gamma vẫn dùng mốc 1, không đổi sang thang hiển thị của Capture One.
- Color Editor: Basic giữ tám nhóm màu cũ. Advanced thêm/xóa/tắt dải màu, chọn tâm hue/saturation bằng bánh xe và kéo biên ngoài để đổi độ rộng. Smoothness 1…30; Hue −30…30; Saturation −100…200; Lightness −100…100. ΔH/ΔS/ΔL cập nhật tại danh sách và dưới thanh kéo.
- Skin Tone: chọn dải màu, Amount HSL, Uniformity HSL và mức sáng đích. Đây là lựa chọn theo màu, không phải nhận diện mặt/da AI.
- Black & White: bật/tắt; sáu độ nhạy Red/Yellow/Green/Cyan/Blue/Magenta −100…100. Split Tones có Hue 0…360 và Saturation 0…100 riêng cho Highlights/Shadows, thanh màu.
- Clarity: Natural/Punch/Neutral/Classic; Clarity/Structure −100…100. Classic giữ hành vi cũ; các phương thức mới thay đổi trọng số tương phản cục bộ.
- Dehaze −100…100; Shadow tone Auto/Manual. Manual chọn hue/saturation của sắc độ vùng tối.
- Vignetting −4…4 EV, tính gain trong ánh sáng tuyến tính; Elliptic on Crop/Circular on Crop/Circular. Hình ellipse theo khung ảnh đang xử lý; hai hình tròn chuẩn hóa theo cạnh ngắn/cạnh dài. Chưa có tọa độ crop gốc riêng để mô phỏng chính xác Circular của Capture One.
- Bỏ Calibration, Legacy adjustments, Effects và Detail khỏi Adjust. Dữ liệu cũ vẫn được đọc/xử lý để không âm thầm đổi các preset đã lưu. Grain/Sharpening vẫn thuộc Refine.

## Pipeline và giới hạn

Các trường mới lưu trong ColorRecipe và đi qua cùng ColorProcessor cho preview/preset. Trường nullable giữ hành vi công thức cũ. B&W mới dùng màu trước khi chuyển xám để sáu độ nhạy có tác dụng; split tones chỉ chạy khi bật B&W. Dải Advanced/Skin Tone là phép biến đổi theo màu nên có thể nướng LUT. Vignetting/Clarity/Structure/Dehaze vẫn bị loại khỏi LUT và live view như nhóm spatial cũ.

Không phải thuật toán Capture One. Kelvin xử lý tương đối trên RGB với mốc 5000 K, không đọc WB RAW hay profile camera. Shot không khôi phục dữ liệu RAW. Các dải màu có thể tác động vật thể có cùng màu da. Chưa có eyedropper hoặc chế độ xem riêng vùng màu được chọn trong Color Editor mới. Các nhãn và khoảng giá trị không phải bằng chứng chất lượng tương đương phần mềm tham khảo.

## Kiểm chứng

- Build Release: 0 warning/error; publish `.local/builds/adjust-tools-2026-10-21/`.
- 190 tests đạt, gồm sáu kiểm thử mới về WB, dải màu, Skin Uniformity, B&W sensitivity/split tones, vignetting, JSON roundtrip và phương thức clarity. Bốn kịch bản harness đạt.
- WPF runtime kiểm tra trên session riêng: render các nhóm, Kelvin tự chuyển Custom, Advanced saturation=200 cập nhật recipe, tab Skin Tone/B&W hoạt động. Ảnh và kết quả ở `.local/adjust-2026-10-21/`.
- Rà ảnh render Exposure/HDR/Advanced/Skin Tone/Split Tones và trang Bộ lọc màu. Chưa kiểm tra thao tác cảm ứng thực tế hay chất lượng ảnh trên booth.
- Chạy bản mới qua `Open-Adjust.cmd`; hoặc `FotoAutomat.Desktop.exe --adjust-settings`.

## Reset và Levels — 22/10/2026

- Thêm biểu tượng mũi tên quay trái ở góc trên phải của 11 nhóm Adjust. Reset chỉ áp dụng nhóm được bấm trên lớp đang chọn, giữ nguyên nhóm khác, trạng thái mở/đóng và vị trí cuộn. Levels/Curve reset toàn bộ kênh trong nhóm; Color Editor reset cả Basic/Advanced/Skin Tone. Nút đặt lại kênh/dải hiện có vẫn dùng để reset riêng.
- Levels: tâm input vẫn bằng InputBlack + (InputWhite − InputBlack) × 0,5^Gamma. Khi kéo biên input, tâm đi theo mà không đổi gamma. Sửa tâm output thành (OutputBlack + OutputWhite)/2; trước đây dấu output lấy nhầm tọa độ tâm input. Đường nối giữa hai tâm phản ánh hai khoảng độc lập. Theo yêu cầu cập nhật, cả hai tâm chỉ hiển thị: không kéo riêng được. Chỉ hai biên trái/phải tương tác; gamma có sẵn được giữ nguyên.
- Publish riêng `.local/builds/adjust-reset-levels-2026-10-22/` để tránh ghi đè bản đang chạy. Build publish thành công. Kiểm tra WPF riêng xác nhận 11 nút reset, reset Exposure không đổi HDR, giữ expansion, kéo hai biên input dịch tâm và kéo output; ảnh render ở `.local/adjust-reset-2026-10-22/`. Chưa kiểm tra cảm ứng vật lý.

## Hút màu Advanced / Skin Tone — 22/10/2026

- Nút pipette bật chọn màu trên preview bằng chuột/cảm ứng; bấm lại hoặc Escape để hủy. Chọn xong tự thoát. Dùng tọa độ tương đối Image nên tính cả zoom/pan và vùng letterbox; không chọn màu từ reference hoặc nền ngoài ảnh.
- Advanced cập nhật dải đang chọn (tạo dải đầu nếu chưa có); Skin Tone cập nhật tâm hue/saturation và target lightness. Dải màu vẫn là quy tắc theo màu, không phải mask da AI.
- Vòng màu hiển thị biên hue, phạm vi saturation và chấm tâm màu. ΔH/S/L là mức chỉnh của dải. RGB/HSL bên dưới là mẫu đã chọn → pixel preview hiện tại, không phải delta riêng của một bước xử lý. RGB 0–255, H độ, S/L phần trăm.
- (Đã thay ngày 24/10, xem mục cuối) Mẫu lấy từ ảnh preview đang hiển thị. Mẫu/điểm đối chiếu chỉ giữ trong phiên UI cho lần chọn gần nhất; quy tắc màu lưu theo recipe. Chưa có chế độ xem riêng mask dải màu.
- Build Release/publish thành công tại `.local/builds/adjust-picker-2026-10-22/`. Kiểm tra WPF: lấy pixel đúng RGB/tọa độ, chọn Advanced/Skin Tone cập nhật tâm dải, bảng RGB cập nhật, hủy chế độ chọn sau khi chọn, render vòng màu. Chưa kiểm tra cảm ứng vật lý.

## Bố cục vòng màu — 23/10/2026

- Advanced/Skin Tone luôn hiển thị vòng màu; Advanced chưa có dải dùng giá trị mặc định để hiển thị, chỉ tạo dữ liệu khi người dùng chỉnh hoặc hút màu. Bỏ nút thêm/xóa dải; giữ bộ chọn khi preset có nhiều dải cũ.
- Vòng màu vuông theo chiều rộng bảng công cụ, nút hút màu 32px ở góc dưới phải. Không mở ứng dụng tự động.
- Build/publish adjust-wheel-2026-10-23 thành công. Render WPF không Window/Show xác minh hai tab có vòng 400px, kể cả Advanced chưa có dải, và không còn nút thêm/xóa. Ảnh ở .local/adjust-wheel-2026-10-23/.

## Vòng chuyển sắc và xem dải chọn — 23/10/2026

- Vùng chọn trên wheel dùng gradient theo hue/saturation, alpha feather từ cùng hàm Weight của dải; Smoothness thay đổi độ mềm biên. Bitmap gradient 320px cache theo thông số chọn.
- Advanced chỉ còn Smoothness/Hue/Saturation/Lightness; Skin Tone chỉ còn Smoothness, Amount H/S/L, Uniformity H/S/L. Bỏ Range hue/width/sat và Target light khỏi UI; các giá trị recipe vẫn được giữ, hue/width chỉnh trên wheel, target lightness lấy khi hút màu.
- View selected color range giữ màu vùng trong dải, chuyển ngoài dải sang xám, feather theo Smoothness. Đây là chế độ xem trên RGB preview hiện tại, không phải mask tại đầu vào từng bước xử lý. Không lưu vào recipe, không sửa ảnh gốc/export/LUT. Thay tab Basic hoặc rebuild bộ công cụ tắt chế độ này. Picker và RGB/HSL tiếp tục đọc bitmap chưa phủ chế độ xem; histogram không bị thay đổi bởi lớp xem.
- Cập nhật này thay giới hạn trước đây “chưa có chế độ xem riêng vùng màu”. Vẫn chưa có bộ đệm tại từng bước pipeline: khi look đổi hue mạnh, vùng preview cuối khác vùng đầu vào của bộ chỉnh màu.
- Publish adjust-range-view-2026-10-23 thành công. Kiểm tra WPF offscreen không Window/Show: 4/7 slider, giữ đỏ/chuyển xanh lá và xanh dương sang xám, không sửa source, bật/tắt khôi phục bitmap và picker đọc nguồn không bị xám. Ảnh render ở .local/adjust-range-view-2026-10-23/.
## Nền vòng màu và bảng số — 23/10/2026

- Vòng màu dùng nền #2B2B2B bo góc nhẹ, viền vùng chọn sáng và icon hút màu xám sáng; phần slider giữ theme hiện tại.
- RGB/HSL xếp 3 hàng: R/H, G/S, B/L, mỗi chỉ số 2 cột số (mẫu đã chọn, preview). Bỏ dòng diễn giải và mũi tên; H vẫn tính theo độ, S/L theo phần trăm, số tối đa 1 chữ số thập phân. Tên accessible phân biệt 2 cột.
- Publish adjust-dark-wheel-2026-10-23 thành công; kiểm tra render offscreen với mẫu thật RGB 183/149/154, xác minh không còn mũi tên. Không tạo/mở cửa sổ ứng dụng.
## Skin Tone, nhiều mẫu và nhóm lựa chọn — 23/10/2026

- Skin Tone có hai cung kéo: trái từ trung tính tới màu đang chọn, điều chỉnh Amount Saturation (-100…200); phải đen dưới/trắng trên, điều chỉnh Amount Lightness (-100…100). Slider cập nhật khi kết thúc kéo. Không đổi thuật toán màu theo Capture One.
- Bỏ checkbox Bật dải màu ở Advanced/Skin Tone. Mỗi lần bấm hút màu rồi chọn ảnh thêm một dải độc lập, tối đa 32 dải mỗi tab. Bộ chọn cho phép quay lại dải trước; RGB/HSL mẫu được giữ riêng theo preset/layer/tab/dải trong phiên UI. Nút hút màu bị khóa khi đủ 32 dải. Không tự bật lại các dải disabled đã có trong preset cũ.
- SkinColors lưu danh sách dải da trong recipe; nếu chưa có trường mới, đọc SkinColor cũ. Các dải xử lý tuần tự trong cùng AdjustToolProcessor, validate từng dải và giới hạn 32. Reset Color Editor xóa cả danh sách mới. Không phải semantic skin mask; dải chồng nhau có thể tác động cộng dồn.
- Black & White: hai tab luôn bấm xem được. Chỉ các trường bên trong bị khóa khi Enable Black & White tắt; backend tiếp tục bỏ qua B&W settings khi chưa bật.
- Match Look: 16 checkbox con thật trong Light & Contrast, Color Adjust, Color Tone. MatchComponents mặc định All giữ hành vi cũ. Checkbox giới hạn các tham số được optimizer phép fit; không chỉ ẩn UI hay xóa thông số sau fit. Khi bỏ hết lựa chọn có hiệu lực, validation chặn Apply. Không thay loss/heuristic/model và không train.
- Publish adjust-multicolor-2026-10-23 thành công, 192 tests đạt và 4 harness scenarios đạt. Test mới kiểm tra lưu/đọc nhiều SkinColors, tương thích SkinColor cũ, tác động độc lập của hai dải và chỉ fit Contrast khi được chọn. WPF offscreen xác minh 2 lần hút màu giữ dải trước, chọn lại dải, hai cung kéo thay thông số, B&W tab/field gating và 16 checkbox con. Không mở Window/Show; chưa thao tác cảm ứng vật lý.
## Phủ kín khung cả ba tab — 23/10/2026

Người dùng xác nhận dải đen xuất hiện ở cả Adjust/Retouch/Refine; bản trước mới sửa Retouch. Cả ba tab nay dùng chung EditorImageViewport.Cover/Apply: cover theo tỷ lệ pixel, giới hạn zoom tối thiểu 1 và pan không vượt mép. Explicit image dimensions + Stretch.Fill tránh letterbox do DPI metadata; các kích thước đã theo tỷ lệ pixel nên không kéo méo. Đồng bộ mask, brush, face/spot overlay và tọa độ hút màu. Refine 100% bị giới hạn bởi mức cover nếu ảnh nhỏ hơn viewport; navigator vẫn giữ overview toàn ảnh. Reference pane vẫn xem toàn reference, không crop reference.

Publish editor-fill-all-2026-10-23 thành công. Kiểm tra WPF offscreen bằng ảnh thật người dùng đã cung cấp: 3 tab × 2 tỷ lệ khung × zoom .25/1/2 và pan cực trị; không lộ mép, tọa độ ảnh khớp overlay. 6 ảnh render ở .local/editor-fill-all-2026-10-23/. Không mở cửa sổ hoặc tắt phiên người dùng đang chạy. Chưa kiểm tra cảm ứng vật lý. Kiểm tra assembly đang chạy cho thấy Retouch ImageRect/Transform trùng bản sửa trước; không kết luận người dùng đang mở bản cũ.
## Khung ngoài và clipping — 23/10/2026

Ảnh phản hồi tiếp theo cho thấy thanh Kết quả màu đen và dải dưới; kiểm tra trước chỉ render imageSurface nên bỏ sót thanh header. Bỏ header Kết quả, đổi resultPane thành Grid phủ kín. Dùng Canvas cho Image ở cả 3 tab: ảnh oversized được layout đủ kích thước trước khi viewport clip, pan qua Canvas.Left/Top thay vì di chuyển RenderTransform cùng layout clip của Image trong Grid. Cover/clamp và tọa độ overlay dùng chung, picker tiếp tục lấy điểm tương đối Image.

Publish editor-edge-fix-2026-10-23 thành công. WPF offscreen trên ảnh thật: cả 3 tab, 2 tỷ lệ khung, zoom .25/1/2, pan cực trị. Kiểm tra pixel sát cả 4 mép không có nền #1C1E1F hay pixel trong suốt; kiểm tra toàn resultPane ở Adjust (không chỉ imageSurface), không có header/padding ăn chiều cao. Render cả khung ngoài ở .local/editor-edge-fix-2026-10-23/. Không mở Window hoặc giành focus. Thử nghiệm này xác minh layout nền, không thay thế thao tác trực tiếp ở mọi cấu hình DPI màn hình.
## Blur & Glow — 24/10/2026

Added below Clarity, above Dehaze, with Mist / Glow / Gaussian tabs and shared group reset. Tabs retain independent settings and can be combined. Defaults are neutral (Mist Amount, Glow Amount and Gaussian Mix = 0). All controls range 0–100 except Warmth −100…100. Radius/Spread are relative units, not pixel values.

Mist: Amount, Spread (35), Haze (20), Preserve Blacks (70). Glow: Amount, Radius (35), Threshold (70), Softness (40), Warmth (0). Gaussian: Radius (20), Mix (0). Settings serialize with ColorRecipe and adjustment layers; old recipes without BlurGlow remain unchanged.

Processing runs after existing spatial Clarity/Structure/Dehaze and before vignette/grain/sharpening, in linear light. Three sliding box passes approximate Gaussian diffusion with sigma up to 2% of the shorter image side. Mist adds diffusion/haze weighted by black preservation; Glow thresholds linear luminance, feathers the selection, spreads it and screen-blends tinted light. Gaussian mixes blurred premultiplied RGB with the original. Alpha is retained; invisible RGB does not bleed into visible pixels. Source is not modified. All effects are removed recursively for live view and LUT baking; LUT export reports Blur & Glow as excluded. This is an original approximation, not Photoshop algorithm parity or RAW optical diffusion.

Verification: isolated Release publish .local/builds/blur-glow-2026-10-24; 223 tests passed, four simulator scenarios passed. Added tests for neutral/JSON/validation, highlight spread/threshold, blur edges/transparent colour isolation, black preservation, cancellation and LUT/live exclusions. WPF offscreen verified group order, all 11 sliders, tab persistence and reset; screenshots and real-photo render in .local/blur-glow-check. One local 960x898 combined render measured 312 ms (not NUC throughput certification). No window shown, no customer-flow files changed, no physical device activity. Full-resolution booth export and physical display acceptance remain separate work.

## Hút màu và xem dải chọn theo đúng bước Color Editor — 24/10/2026

- Pipeline tách thành `ColorProcessor.BeforeColorEditor` (LUT, tone gốc, Tone, Levels, Curves, HSL, Color Balance) và phần còn lại. Ảnh xuất không đổi; có test xác nhận.
- `ColorEditorStage.RangeInput` dựng lại đúng pixel mà một dải nhận được: ảnh mẫu sau Retouch → các bước trước Color Editor → các dải bật đứng trước nó (Advanced theo thứ tự, Skin Tone chạy sau toàn bộ Advanced). Với layer: bắt đầu từ ảnh base đã trộn các layer phía trên.
- Hút màu lấy mẫu trên `RangeInput` của dải mới (nối vào cuối danh sách) tại cùng tọa độ chuẩn hóa, không còn lấy từ preview cuối. Chưa nạp ảnh mẫu thì quay về lấy trên preview.
- View selected color range dùng `ColorEditorStage.Coverage`: trọng số của dải trên màu nó thực nhận, nhân mask × opacity của layer. Vùng giữ màu trên preview là vùng thực sự bị dải tác động; các bước sau (dải sau, Clarity, glow, vignette, grain) không làm lệch vùng. Kết quả được cache theo preview, look, layer và dải.
- Bảng RGB/HSL vẫn so mẫu (đầu vào dải) với pixel preview cuối.
- Test: `ColorEditorStageTests` (5) trong tổng 228/228. Chưa render kiểm tra trên WPF.
