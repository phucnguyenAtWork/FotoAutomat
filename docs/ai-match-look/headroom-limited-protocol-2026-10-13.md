# Headroom-limited correction — dự đoán đăng ký trước

Trạng thái: **chưa chạy phép đo excursion trong mask da hoặc nhánh headroom-limited**. Giữ α=1 của `exposure-normalized-correction-2026-10-13-001` làm ứng viên dev đã chọn; không chọn lại α. Chỉ suy luận, không đổi checkpoint/gate hoặc mở test. Protocol này được ghi sau khi biết kết quả MAE da cũ, nhưng trước khi xem metric excursion theo mask da.

## Cơ sở và giới hạn đã biết

Opus báo α=1 có 41/128 ca tăng skin MAE >2/255 so với joint_mapped, lớn nhất +10,63; cool16/16, high_contrast8/16. Đây là số liệu được cung cấp để định hướng, cần tái tính từ cases.json trước phân nhóm. Tiêu chí da trung bình cũ che phân bố; việc vượt qua tiêu chí dev cũ không chứng minh da ổn định.

Các ca là8source ×8look ×2reference, không phải128người độc lập. Mask da là mask ước lượng của pack, giữ nguyên cho mọi nhánh.

## Dự đoán kiểm chứng trước can thiệp

**H1:** các ca có skin MAE tăng >2/255 ở α=1 có tỷ lệ pixel excursion trong mask da cao hơn các ca còn lại. Nếu chênh lệch giữ được khi xét nội dung/look, headroom-limit có cơ sở để thử giảm lỗi da. H1 là liên hệ thống kê, chưa phải khẳng định nhân quả hoặc bảo đảm limiter hiệu quả.

### Định nghĩa khóa trước

- Độ tăng lỗi da của ca i: `delta_skin_i = skin_MAE(norm_a10_mapped) - skin_MAE(joint_mapped)`, theo metric cũ sau mapping. Nhóm A: delta >2/255; nhóm B: delta ≤2/255. Không đổi nhóm theo kết quả limiter.
- Đo excursion trên output float **sau gain/BW, trước gamut mapping**, cùng vị trí raw của báo cáo trước. Đo cả joint raw và norm_a10 raw để phân biệt lỗi có sẵn với lỗi correction thêm vào.
- Pixel excursion nếu **ít nhất một kênh** <−1e-6 hoặc >1+1e-6. Tỷ lệ = số pixel excursion trong mask da / tổng pixel mask da. Không nhầm với tỷ lệ giá trị kênh RGB trong quality gate cũ.
- Mức excursion mỗi pixel = max trên3kênh của `max(−RGB, RGB−1, 0)`, nhân255 để báo đơn vị. Báo mean/p95/max trong toàn mask và mean trên pixel excursion (null nếu không có).
- Báo tăng tỷ lệ excursion norm_a10−joint và phần diện tích excursion mới (norm lỗi, joint không lỗi), tránh gán toàn bộ excursion có sẵn cho correction.
- Vùng mask trống trả null, ghi mẫu số; không thay bằng0. Không clamp trước phép đo. Không lấy target để quyết định limiter; target chỉ để chấm điểm/phân nhóm chẩn đoán.

### So sánh đã định trước

1. Toàn tập: số ca mỗi nhóm, mean/median tỷ lệ excursion mỗi ca, hiệu A−B, mean/median tăng tỷ lệ và mức excursion. Mỗi ca trọng số bằng nhau; không gộp tất cả pixel làm ảnh có mặt lớn chi phối.
2. Báo riêng8look, nhấn mạnh **cool, low_key, high_contrast**. Mỗi look ghi số ca A/B, dữ liệu từng source/reference và delta_skin.
3. Với look có cả A và B, tính hiệu A−B riêng trong look, rồi trung bình ngang nhau giữa các look đủ điều kiện; ghi look bị loại vì thiếu nhóm. Không gán0 hoặc suy ra không có quan hệ khi thiếu nhóm.
4. Nếu cool vẫn16/16 thuộc A, **không thể so A/B trong cool**. Báo phân bố excursion của16ca, số ca tăng lỗi da dù không có excursion, và quan hệ liên tục giữa delta_skin và tỷ lệ excursion như chẩn đoán phụ. Không dùng nhóm màu khác làm bằng chứng nhân quả riêng cho cool.
5. Báo Spearman delta_skin với tỷ lệ excursion trên128ca và theo look khi có biến thiên, cùng kết quả gộp theo8source để nhận diện phụ thuộc mẫu. Không coi128ca là128quan sát độc lập; không tuyên bố ý nghĩa thống kê từ p-value mặc định.

### Cách đọc kết quả đã định trước

- Bằng chứng mô tả ủng hộ H1 khi mean và median A−B toàn tập đều dương, đồng thời chênh lệch trung bình trong các look có đủ hai nhóm dương. Chênh lệch rất nhỏ vẫn phải báo độ lớn; dấu dương không đủ gọi là cải thiện hữu ích.
- Nếu chỉ gộp toàn tập dương nhưng trong look không dương, coi là **hỗn hợp/có thể bị thành phần look gây nhiễu**, chưa dùng H1 để giải thích lỗi.
- Nếu các chênh lệch không dương, H1 không được ủng hộ trên tập này. Không tự kết luận xung đột phong cách: mask sai, tone curve, hue/saturation hoặc lỗi không gây excursion cũng có thể góp phần.
- Nếu cool tăng lỗi da nhưng phần lớn pixel da không excursion, ưu tiên protocol riêng để kiểm tra correction kéo ngược tông lạnh. Các ứng viên gồm giảm trọng số correction trên da hoặc chỉ sửa luma trên da; **chưa triển khai/chọn**, cần đo giữ phong cách, biên mask và độ ổn định theo mặt.
- Kể cả H1 được ủng hộ, chỉ ablation limiter đối chứng cùng ca mới cho biết giới hạn headroom có cải thiện da hay không. H1 âm không chứng minh mọi headroom-limit đều vô ích.

## Ràng buộc cho lượt limiter kế tiếp

Giữ baseline joint_mapped và norm_a10 làm đối chứng, cùng source/reference/mask/gain/BW. Limiter chỉ điều chỉnh biên độ correction theo RGB hợp lệ, không chọn theo tên look, target hoặc nhóm A/B. Cần ghi chính xác công thức limiter, vị trí áp dụng và cách xử lý baseline đã vượt miền trước khi chạy; không sửa chúng sau khi xem kết quả rồi gộp thành cùng một lượt.

Giữ tiêu chí dev trước: neutral giảm≥1,5 so baseline, mỗi look không tăng>1,0, macro không tăng, da trung bình không tăng>0,5. Bổ sung tiêu chí phân bố cho **sàng lọc nghiên cứu**, chưa phải nghiệm thu khách:

- Giảm ít nhất25% số ca delta_skin>2 so norm_a10 (nếu tái hiện41ca thì phải≤30ca).
- Mức tăng da lớn nhất và p95 delta_skin không cao hơn norm_a10; không tạo ca mới delta_skin>2 trong nhóm B đã khóa.
- Báo riêng các điều kiện này ở cool/low_key/high_contrast; không cho số ca >2 tăng trong bất kỳ nhóm nào trong ba nhóm đó.
- Báo raw và mapped gate tách biệt; đạt tiêu chí phân bố không thay thế quality gate cũ. Không nới ngưỡng hoặc chọn lại α theo kết quả này.

## Đối chứng cùng độ lớn correction — bổ sung trước chạy

**H2:** limiter chỉ có giá trị riêng nếu tốt hơn việc thu nhỏ correction đều với cùng độ lớn tác động. Việc đạt ≤30 ca da tăng lỗi>2 tự nó không chứng minh H2: norm α=0,5/0,75 đã có ít ca hồi quy hơn α=1. Giữ α=1 là ứng viên gốc; các hệ số dưới đây chỉ là đối chứng độ lớn, không phải chọn lại α theo chất lượng.

### Đo độ lớn trên output, không dùng target

Gọi `z0` là output joint trước mapping, `zL` là output limiter trước mapping và `zβ` là output correction normalized với một hệ số β đều cho mọi pixel/kênh. Cả ba dùng **cùng gain g0, BW, source/reference và vị trí xử lý**. Định nghĩa `A_i(z)=mean_RGB,pixel(abs(z-z0))*255` cho ca i. Không clamp/mapping trước phép đo; không dùng skin mask, tên look hoặc target để khớp β. Báo thêm độ lớn sau mapping như chẩn đoán, không đổi đại lượng matching sau khi xem kết quả.

Đăng ký hai đối chứng:

1. **Uniform-global (chính):** một β duy nhất trong[0,1], dùng chung128ca, sao cho mean_i A_i(zβ) bằng mean_i A_i(zL). Mỗi ca có trọng số bằng nhau. Chỉ khớp độ lớn output; không tối ưu MAE/neutral/da để tìm β.
2. **Uniform-per-case (kiểm tra bổ sung bắt buộc):** mỗi ca có β_i trong[0,1], đều trong toàn ảnh, khớp A_i(zβ_i)=A_i(zL). Nhánh này loại thêm lợi ích do limiter vô tình phân phối cường độ khác nhau giữa ảnh. Nó là đối chứng theo ảnh, không phải một preset cố định.

Tìm hệ số bằng lưới cố định257điểm trong[0,1], sau đó bisection tối đa40bước trên khoảng đầu tiên có đổi dấu sai số độ lớn; tolerance khớp1e-4/255. Nếu có nhiều nghiệm chọn β nhỏ nhất trong các khoảng đã phát hiện; ghi giới hạn độ phân giải lưới. Nếu không đạt tolerance hoặc không có nghiệm trong miền, ghi unmatched và sai số thực tế; không nới tolerance hoặc khẳng định đã so cùng độ lớn. Nếu độ lớn limiter và correction đều bằng0, dùng β=0 và ghi zero-effect. Không nội suy điểm chất lượng để chọn β.

### Tiêu chí lợi ích riêng của limiter

Ngoài mọi tiêu chí dev/phân bố đã nêu, so limiter với **từng đối chứng matched**:

- Toàn tập: skin MAE trung bình thấp hơn, số ca delta_skin>2 thấp hơn; p95 và mức tăng da lớn nhất không cao hơn. Delta_skin vẫn lấy so joint_mapped, không đổi mốc khi so nhánh.
- Riêng từng look **cool, low_key, high_contrast**: skin MAE thấp hơn, MAE toàn ảnh không cao hơn, số ca delta_skin>2 không tăng. Không dùng cải thiện của look này bù suy giảm của look khác.
- Sai số số học cho so metric là0,001/255; số ca so bằng số nguyên. Đây không phải ngưỡng nhìn thấy hoặc ý nghĩa thống kê. Báo đầy đủ độ lớn chênh lệch.
- Nếu chỉ thắng uniform-global nhưng không thắng uniform-per-case, chỉ kết luận có lợi ích do phân bố cường độ giữa ảnh, **chưa có bằng chứng giới hạn theo pixel tốt hơn thu nhỏ đều trong ảnh**.
- Nếu không thắng đối chứng matched hoặc matching không hợp lệ, không công nhận limiter là cải tiến riêng, dù đạt≤30ca. Không thay α hoặc chọn hệ số matching bằng target để cứu kết quả.

### Đường đánh đổi đã định trước

Vẽ trục X = neutral MAE (thấp hơn tốt), trục Y = số ca delta_skin>2 (ít hơn tốt). Đặt các điểm norm α=0,25/0,5/0,75/1 từ dữ liệu đã có, joint_mapped α=0, limiter, uniform-global và uniform-per-case. Ghi β hoặc phân bố β_i, mức matching error. Các điểm cũ phải dùng cùng128case ID và cùng định nghĩa metric; xác minh parity trước khi ghép.

Đánh dấu các điểm bị một điểm khác trội hơn trên cả hai trục; đường nối α chỉ minh họa, không giả định kết quả ở α chưa chạy hoặc dùng đường nội suy làm nghiệm thu. Nếu limiter chỉ nằm trên cùng vùng đánh đổi với α nhỏ hơn, không gọi là cải thiện giữ phong cách. Biểu đồ hai trục không thay thế kiểm tra da/highlight/per-look/raw gate.

## Đầu ra cần lưu

Protocol snapshot/hash trước chạy; hash checkpoint/manifest/plan; bảng từng ca gồm source/reference ID, mask coverage, delta_skin, nhóm A/B, excursion baseline/norm/limiter; tổng hợp theo look/source; ảnh excursion overlay chọn theo quy tắc định trước (ca delta_skin lớn nhất mỗi look, kèm ca median). Ghi rõ số ca/nguồn, dev dùng lại nhiều lần, chưa phải dữ liệu booth hiệu chuẩn hoặc blind test. Không chạy lại fine-tune.

Bổ sung: độ lớn output từng ca, β_global/β_i, matching error và unmatched flags; metric của cả hai uniform controls; biểu đồ neutral–số ca da tăng lỗi>2, kết luận H2 riêng với H1. Trạng thái bổ sung này: chưa chạy limiter hoặc matching, chưa chọn β.
