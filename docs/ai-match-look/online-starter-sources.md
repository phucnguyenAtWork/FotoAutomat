# Nguồn online cho Phase 1

Đối chiếu 05/10/2026 theo yêu cầu tìm nguồn thay thư mục ảnh riêng. Chưa tải ảnh hoặc chạy inventory thật. Đây là shortlist nguồn, không phải tập dữ liệu đã duyệt.

| Nguồn | Mục đích ban đầu | Giới hạn |
| --- | --- | --- |
| [PST50](https://huggingface.co/datasets/zrgong/PST50) | Ưu tiên benchmark Match Look: content, reference, target và protocol paired/unpaired | Card CC-BY-4.0; giữ attribution và kiểm tra gói thực. Giữ riêng benchmark, không train. Có Rec.709/log; không coi tất cả sRGB. Không thay tập chân dung booth |
| [raw.pixls.us](https://raw.pixls.us/) | Kiểm kê camera, RAW, EXIF và lỗi định dạng | Chọn từng file có license xác minh; RAW chưa được inventory v1 decode. Không phải bộ cặp tone camera chuẩn |
| [Wikimedia Commons](https://commons.wikimedia.org/) | Chọn ảnh màu portrait/scene để smoke-test import/reference | Kiểm tra license từng trang File, attribution và quyền hình ảnh người; không nhầm CC0 metadata với license ảnh. Không có target hoặc nhãn da chuẩn mặc định |

Một ví dụ cảnh quan có trang File ghi CC0: [Nature Landscape (248036123)](https://commons.wikimedia.org/wiki/File:Nature_Landscape_(248036123).jpeg). Chưa tải hoặc xác nhận chất lượng ảnh; chỉ là asset candidate. Không lấy chân dung lịch sử đen trắng thay dữ liệu màu da hiện đại.

Không mặc định dùng nguồn ảnh stock miễn phí cho AI: [Pexels](https://help.pexels.com/hc/en-us/articles/900005880463-What-are-the-Terms-and-Conditions) nêu hạn chế thu thập cho ML/AI khi chưa được phép; [Unsplash](https://unsplash.com/terms) có hạn chế dùng ảnh với dataset AI/ML. [FFHQ](https://github.com/NVlabs/ffhq-dataset) công bố dataset theo CC-BY-NC-SA-4.0, ảnh có license riêng; không chọn làm nguồn mặc định train thương mại. PPR10K cũng không được tự duyệt thương mại theo Agreement đã ghi trong data-readiness.

Kế hoạch lấy mẫu: subset ảnh tĩnh PST50 trước, giữ quan hệ source/reference/target và source URL, split evaluation-only. Với portraits, ưu tiên bộ có quyền cụ thể/ảnh tự thu; các ảnh Commons nếu được chọn chỉ giúp kiểm tra workflow, chưa chứng minh face/skin quality. Cần thêm nhãn mặt/da thủ công và held-out booth data trước benchmark sản phẩm. Không tải video hoặc toàn bộ archive chỉ để thử inventory.

Mỗi file tải sau này cần source page, direct URL, tác giả, license URL, ngày truy cập, hash và role. Bản tải đưa vào `.local/`, không commit; quyền train mặc định chưa duyệt.
