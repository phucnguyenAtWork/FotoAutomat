# Match Look — so sánh reference, 15/10/2026

Theo yêu cầu mới của người dùng, Match Look không còn tùy chọn `Apply as new Layer`. Publish luôn tạo recipe toàn cục qua `MatchRecipeComposer.Compose(..., asLayer: false)`. Không xóa khả năng Layers/Masks của Adjust hoặc sửa preset cũ đã có layer.

Thêm reference sẽ tự mở hai khung: Reference bên trái, Preview bên phải. Checkbox Reference đóng/mở khung so sánh. Combo chọn một trong các reference để xem; thao tác này không đổi tập reference dùng để fit. Clear references đóng khung và bỏ ảnh cũ. Ảnh reference hiển thị nguyên bản đã qua shared image loader, không áp preset của ảnh nguồn. Zoom/pan/mask vẫn chỉ tác động preview.

## Kiểm chứng

- Build WPF vào `.local/builds/match-compare/`: 0 warning, 0 error. Build đường dẫn Release mặc định bị khóa bởi app đang chạy; không đóng phiên người dùng.
- 139 test đạt, 0 skip. Hai ca mới chạy v4/v5a: fit A, fit B, fit lại A; kiểm tra ảnh đầu ra A giống nhau, score giống nhau và pixel nguồn không bị sửa. Dữ liệu tổng hợp, một pass; không chứng minh chất lượng ảnh thật hoặc trạng thái WPF khi đổi preset.
- Chưa kiểm chứng bố cục bằng thao tác WPF trên màn hình.

## Giới hạn màu

Không thay đổi thuật toán hay loss. Engine hiện fit riêng theo ảnh nguồn và reference; cùng reference trên hai source không đảm bảo cùng recipe. Phàn nàn về màu trên ảnh thật vẫn chưa được giải quyết bằng thay đổi UI này. Chưa có bằng chứng lỗi ngẫu nhiên ở engine qua hai test mới; chưa loại trừ lỗi trạng thái UI, Retouch/Refine hoặc khác options.

Không tải model, train, thay đổi AGENTS.md hay xóa file. Hướng v5b vẫn cần duyệt model/giấy phép trước tải và tích hợp.

## Cập nhật: header Reference và gỡ LUT

- Bỏ combo trắng dài. Header tối cùng chiều cao Preview: Reference, tên file rút gọn (tooltip tên đầy đủ); chỉ khi nhiều reference mới hiện ‹ số/tổng ›. Chuyển ảnh xem không thay tập reference để fit.
- Chọn preset có `Lut` sẽ hiện **Xóa LUT (.cube)** dưới bộ chọn preset. Hỏi xác nhận trước khi gỡ bản nháp và bản đã lưu theo PresetId. Không xóa file .cube gốc, không phát hành các draft khác. Nếu xóa mặc định thì chuyển về preset đầu tiên; nếu thư viện rỗng thì tạo preset tự nhiên. Xóa lịch sử Undo/Redo sau khi gỡ để không hồi sinh preset đã xóa; hộp xác nhận ghi rõ không có Undo.
- Chưa thực hiện xóa preset nào của người dùng trong lượt phát triển này.
- Build `.local/builds/match-reference-clean/`: 0 warning, 0 error. 142 test đạt; test mới kiểm tra bảo toàn preset khác, chỉ số mặc định, thư viện cuối cùng và từ chối gỡ preset không có LUT. Chưa kiểm chứng tương tác WPF trên màn hình.

## Cập nhật 16/10 (Opus): gọn bố cục, mặc định và xóa preset

Theo phản hồi người dùng (bố cục rối, "Default preset" như không có tác dụng, không xóa được preset thường):
- **Một hàng quản lý preset** ở đầu cột phải: `[chọn preset ▾] [★/☆ Mặc định] [Xóa]`.
- **Mặc định:** "☆ Mặc định" đặt preset đang chọn làm tông mặc định của màn hình khách (lưu preset nếu đang là bản nháp). Preset mặc định hiện "★ Mặc định" (nút bị khóa) và thumbnail có dấu ★. Đã bỏ ô "Default preset" phía trên các tab và nút "Tông mặc định" vì trùng chức năng. Ô cũ chỉ đổi mặc định của booth, không có phản hồi trong trình chỉnh nên trông như không có tác dụng.
- **Xóa:** dùng cho **mọi** preset (Matched look, preset tự tạo, LUT nhập), có xác nhận, không có Undo. `ColorWorkspace.RemoveLut` được thay bằng `RemovePreset`. Xóa preset mặc định thì preset còn lại đầu tiên thành mặc định; thư viện không bao giờ rỗng. File .cube gốc không bị xóa.
- **Footer:** chỉ còn "Nhập preset (.xmp/.cube)" và "Lưu preset", chia đều hai cột.
- **Match Look:** bỏ "Save as Preset" (trùng "Lưu preset"); Export .cube và Apply nằm cùng một hàng.
- Thumbnail preset hẹp hơn (128 px), tên dài bị cắt có tooltip.

Kiểm chứng: build 0 cảnh báo, 143 test đạt (`PresetRemovalTests` thay `LutRemovalTests`, thêm ca xóa preset thường). **Chưa kiểm tra bố cục trên màn hình**: lần chụp tự động bị hủy vì người dùng đang dùng máy.

## Sửa 16/10 (Opus): Apply ghi đè Matched look cũ

**Lỗi:** `MatchLookTool` nhớ `draftId` của bản nháp đầu tiên và không bao giờ xóa. Sau khi bản nháp đã được lưu thành preset, Apply với reference mới vẫn ghi đè đúng preset đó. "Lưu preset" với tên mới thì tạo bản sao nhưng để lại bản nháp "Matched look" thừa.

**Quy tắc mới** (`MatchLookDrafts` trong Core, có test):
- Apply chỉ cập nhật **bản nháp chưa lưu** của lần match hiện tại. Bản nháp đã lưu, đã bị xóa, hoặc khi đổi/xóa reference → Apply tạo **"Matched look N"** mới (số tăng dần). Preset đã lưu không bao giờ bị matching sửa.
- Chỉ **Apply** được tạo preset mới. Kéo Impact hay đổi ảnh đích chỉ cập nhật bản nháp chưa lưu; nếu bản nháp đã được lưu thì kết quả match cũ bị bỏ, phải Apply lại.
- "Lưu preset": bản nháp chưa lưu → đổi tên tại chỗ. Preset đã lưu mà đổi tên → lưu bản sao, giữ nguyên bản gốc.

Kiểm chứng: 145 test đạt (thêm `MatchLookDraftTests`); Desktop build vào `.local/builds/match-drafts-2026-10-16/` 0 cảnh báo (thư mục Release mặc định bị khóa vì app người dùng đang mở). Chưa thử trên màn hình.
