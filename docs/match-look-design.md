# Match Look — yêu cầu và hướng triển khai, 03/10/2026

> Cập nhật quyết định 05/10/2026: người dùng chọn CanonCGT làm nền phát triển Match Look và thư viện Digital Camera Looks. Xem [kế hoạch dữ liệu](ai-match-look/data-readiness-2026-10-05.md). Đây là quyết định hướng phát triển, chưa phải model đã tích hợp hoặc benchmark đạt.

Trạng thái cập nhật 04/10/2026: baseline [Paired Match v1](paired-match.md) trong Core/benchmark và [Reference Match v2/v3](reference-match.md) trong Adjust tối ưu recipe đã chạy. GUI đã bỏ Match method/Paired; Reference nhận ảnh khác cảnh/tỷ lệ bằng so quantile RGB/chroma, có hue/saturation guard và ưu tiên vùng mặt khi Windows nhận diện được ở cả hai ảnh; chưa có semantic skin masks hoặc model AI Match Look. Tài liệu này không phải bằng chứng đã có AI hoặc đạt độ giống màu trên 90%.

## Yêu cầu đã xác nhận

Nghiên cứu cập nhật 05/10/2026: [AI Match Look — nguồn và ứng viên](ai-match-look/research-2026-10-05.md), [kế hoạch dữ liệu/train/benchmark](ai-match-look/training-plan.md). Đề xuất thử CanonCGT trước, đối chiếu PETAL/D-LUT; chưa chọn model sản xuất hoặc train. Người dùng dự kiến máy văn phòng nhỏ, cấu hình còn xem xét: phải đo CPU và tách tạo look khỏi áp preset cho khách.

Người quản trị đưa ảnh tham khảo vào, nhận công thức màu có thể sửa bằng Adjust và lưu thành preset bộ lọc. Mục tiêu là độ giống màu trên 90%, có hỗ trợ tinh chỉnh thủ công. Người dùng sẽ gom dữ liệu màu của nhiều digital camera để xây thư viện nhiều tone.

Phải giữ ảnh gốc, preview trước/sau, nhiều ảnh kiểm tra và preset dùng lại. Không loại bỏ hướng AI hoặc thay mục tiêu bằng histogram matching đơn giản.

## Diện mạo màu và thông số gốc

Capture One mô tả Match Look là AI tạo điều chỉnh bằng các công cụ của ứng dụng, với nhóm Normalize, Light & Contrast, Color Adjust và Color Tone; các giá trị sinh ra có thể xem và sửa. Không có mô tả rằng module khôi phục duy nhất bộ thông số máy ảnh gốc. [Tài liệu chính thức](https://support.captureone.com/hc/en-us/articles/22188770298269-Match-Look-Tool).

Suy luận kỹ thuật cho FotoAutomat: một ảnh JPEG hoàn thiện không xác định duy nhất công thức đã tạo nó. Ánh sáng, vật thể, cảm biến, profile, cân bằng trắng và xử lý sau chụp cùng ảnh hưởng pixel. EXIF có thể cung cấp metadata được ghi, nhưng không thay thế ảnh gốc/profile hoặc chứa toàn bộ tone. Vì vậy engine phải **ước lượng công thức tái tạo màu**, không gắn nhãn các số ước lượng là thông số gốc đã đọc được.

## Hai loại dữ liệu, cùng một workflow

| Dữ liệu | Khả năng và hạn chế |
| --- | --- |
| Ảnh tham khảo bất kỳ + ảnh booth | Học diện mạo màu giữa nội dung khác nhau. Cần chọn/khớp vùng da, màu trung tính, tối/sáng; khác cảnh làm việc đo độ giống khó hơn. Không so pixel toàn ảnh rồi gọi là độ chính xác màu. |
| Ảnh đầu vào + ảnh mang tone mong muốn của cùng cảnh | Dùng cặp ảnh căn chỉnh để tìm phép biến đổi màu. Thu nhiều cảnh/điều kiện sáng để tránh học riêng một ảnh; giữ tập ảnh kiểm thử chưa dùng để fit. Nếu ảnh lệch hình học phải căn chỉnh hoặc loại cặp trước khi đo. |

Người dùng đã xác nhận ngày 03/10/2026 sẽ gom **cả hai dạng dữ liệu**. Không có cặp ảnh vẫn cần hỗ trợ Match Look tham khảo; chế độ có cặp là hướng tăng độ tin cậy cho thư viện màu máy ảnh. Cả hai chế độ phải tạo draft có thể sửa trong Adjust và lưu vào thư viện preset chung.

## Tách camera đầu vào khỏi tone mong muốn

Một mục thư viện cần biết camera **đầu vào của booth** và camera/profile **tham khảo tạo tone**. Không dùng tên camera như một phép biến đổi màu chung cho mọi điều kiện.

Đề xuất lưu: tên tone, hãng/model tham khảo, picture style/film simulation nếu biết, model camera booth, định dạng đầu vào, color space/profile nếu biết, WB và điều kiện ánh sáng, nguồn tham khảo, phiên bản engine, công thức ColorRecipe, LUT bổ sung nếu cần và kết quả kiểm thử. Metadata không có phải để Unknown, không suy đoán thành dữ liệu thật.

Ảnh đầu vào phải được chuẩn hóa color space và xử lý orientation; pipeline hiện tại chưa có quản lý ICC/RAW hoàn chỉnh nên đây là việc phải kiểm chứng trước khi cam kết chất lượng đa camera. Tránh học màu trên ảnh đã áp preset/Retouch/Refine ngoài ý muốn.

## Đầu ra và UX đề xuất

Workflow: **Reference → Target photos → Match → Review in Adjust → Save preset**.

* Một/nhiều ảnh tham khảo, ảnh booth và thumbnail kiểm tra cùng một công thức. Có công cụ chọn vùng tương ứng thủ công trước khi có semantic mask AI.
* Chọn nhóm Tone/Color/WB để học; xem tiến độ, hủy chạy và thông báo lỗi rõ ràng. Trung tính trước khi Apply, không tự đổi preset booth.
* Sinh draft ColorRecipe cho Exposure/Contrast/HDR, Curves, HSL và Color Grading; quản trị sửa tiếp bằng Adjust. Đánh giá trước khi Save vào thư viện preset hiện có.
* Phân biệt **chuẩn hóa từng ảnh** (Exposure/WB do điều kiện chụp) và **tone dùng lại**; không tự đóng băng bù sáng của một ảnh vào mọi ảnh tương lai. Cho người quản trị chọn giữ WB/Exposure nếu đó là ý đồ tone.
* LUT 3D là đầu ra bổ sung cho biến đổi màu khó biểu diễn bằng thanh. Nếu có LUT, cần hiện rõ thanh chỉ mô tả phần công thức, không giả vờ mọi hiệu ứng LUT đều quy ngược về thanh. LUT không mã hóa làm mịn da, sharpen hoặc hiệu ứng phụ thuộc vị trí.

## Mục tiêu trên 90% và nghiệm thu

Đây là mục tiêu sản phẩm đã xác nhận; **chưa có định nghĩa phép đo/tolerance**, không tự tạo điểm confidence 90% từ độ giống histogram.

Đề xuất đánh giá trên tập ảnh giữ riêng: sai khác màu theo vùng tương ứng (ví dụ Delta E), màu da và màu trung tính, clipping, giữ chi tiết, tính nhất quán nhiều cảnh và đánh giá trước/sau bởi quản trị. Định nghĩa ngưỡng chấp nhận và cách quy đổi thành mục tiêu trên 90% phải thống nhất trước khi báo đã đạt. Kết quả tốt trên một cặp ảnh hoặc một chart không chứng minh tốt với mọi camera/ảnh tham khảo.

## Thứ tự công việc

1. Chuẩn bị dữ liệu và chuẩn color space; dựng benchmark, preview nhiều ảnh và lưu kết quả.
2. Engine baseline minh bạch: thống kê màu/vùng và tối ưu tham số ColorRecipe qua chính bộ xử lý FotoAutomat. Dùng để đối chiếu, chưa gắn nhãn AI hoặc coi đã hoàn thành mục tiêu.
3. So sánh phương pháp AI, semantic matching và LUT; khảo sát D-LUT cùng các ứng viên khác. Chưa chọn model. Kiểm tra giấy phép thương mại, tốc độ/bộ nhớ và chạy offline trước khi tích hợp.
4. Bổ sung fit trên cặp ảnh và profile đầu vào theo camera; thử nhiều tone và cảnh chưa dùng để fit.
5. Quản trị tinh chỉnh, lưu preset phiên bản, thử lại nhiều ảnh trước khi phát hành vào booth. Camera SDK và xuất full resolution vẫn là hạng mục riêng chưa có.
