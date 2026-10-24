> Cập nhật UI/UX 02/10/2026: xem [tiến độ và giới hạn hiện tại](ui-progress.md). Bảng máy ảnh/bản nháp màu/thông tin/nhật ký đã có UI; tích hợp thiết bị và xử lý ảnh vẫn chưa hoàn tất.

# Operator workspace

Run on Windows from the repository root:

    dotnet run --project src/FotoAutomat.Desktop -c Release -- --admin

Close an older FotoAutomat instance before rebuilding or opening the app. The grayscale workspace has seven sidebar groups: Dashboard, General, Capture configuration, Print layouts, Print settings, Personal and Application. Device operations remain simulated.

## Print layout editor

A layout is one customer-facing design, independent of the physical printer sheet. A strip is 2 x 6 inches (200 x 600 internal units, 600 x 1800 pixels at 300 DPI); a full design is 4 x 6 inches (400 x 600 units). Importing a 1:3 PNG keeps the original bytes and displays one strip. A 2:3 PNG remains one full design. Import never duplicates artwork or enables a cutter.

### Editing workspace

- The left panel adds customer-photo slots, PNG/JPEG decoration images (including an existing QR image), text, rectangles and ellipses. It also edits the page background using a #RRGGBB value and shows the current design dimensions. Images are embedded in the saved layout, up to 5 MB per decoration.
- The center canvas supports 10–400% zoom via buttons, slider, Ctrl+mouse wheel, 100% and Fit. Scrollbars reveal areas outside the viewport. Zoom changes only presentation, not saved geometry. Pointer dragging uses logical canvas coordinates at every zoom level.
- Presets include one three-photo strip, four-photo grid, two-photo design and one-photo design. Presets and blank pages work without PNG artwork.
- The right inspector edits position, size, photo number, text content/font size, shape/text color, aspect lock, rotation, borders and shadows. Geometry fields display pixels at 300 DPI. Rotation is validated against design bounds before saving.
- Slots/elements support duplication, deletion, lock/unlock, layer ordering, horizontal/vertical alignment and vertical gap distribution among at least three unlocked elements sharing the selected X position.
- Up to 16 photo slots and 64 total elements are supported. Decoration elements do not require customer-photo assignment. Layer order, page background and design width are also used by the customer preview.
- Undo/redo retains up to 30 edit states. Each pointer drag is one state. Artwork, geometry, decorations and background are included; zoom is excluded.
- Sample preview hides editing handles. Muted numbered backgrounds distinguish photo slots in edit mode; sample imagery is synthetic.
- Opaque frame PNGs sit underneath photos; transparent frame overlays sit above them. Import does not detect the photo openings; the operator positions the slots.
- Original SVG assets and matching WPF DrawingImage resources provide the editor icons without an added rendering dependency.

### Saving and older files

“Lưu mẫu” saves into LocalAppData/FotoAutomat/Operator/Layouts and updates editor.fotolayout for restart restoration. The saved-layout dropdown reopens these layouts. “Mở mẫu” imports external .fotolayout files. “Áp dụng” saves the current layout and publishes it. Frame library: “Phát hành” copies a saved frame into Operator/Published (fixed copy; “Phát hành bản mới” after editing, “Ngừng phát hành” removes it; deleting a frame also unpublishes it). Customers choose among published frames of the size they picked. The old single active.fotolayout is migrated once into a published frame and renamed to active.fotolayout.migrated.

Replacing or closing a dirty layout offers Save, Discard and Continue editing. Canceling or failing a save leaves the window open.

Old paired layouts are converted in memory only when both slot halves and both artwork halves are identical. The editor then displays the left strip once. The original file is not modified until an explicit save. Nonidentical legacy designs remain intact instead of silently losing content.

### Printer setup

The Máy in page stores paired-strip/cut intent and the number of sheets (1–10) in printer.json. “Kiểm tra kế hoạch in” validates the current design: paired mode requires a single-strip design and plans two placements at x=0 and x=200 on a 4 x 6 sheet. The source layout and customer preview remain a single design. Full designs require paired mode to be off.

This is saved configuration and print-placement planning only. It does not configure the DNP driver, operate its cutter, submit a physical job, or render a production print raster. DNP driver integration and hardware alignment checks remain pending.

QR creation from a sharing link and dynamic session-data fields are not implemented. Existing QR PNG/JPEG images can be placed like other images; this does not create a sharing service.

## Verification

Build on Windows; test the actual 1:3 hi.png import, zoom without geometry changes, text/image/shape editing, save/reopen, legacy duplication detection and customer photo gates. Exercise wide and minimum-size windows. Test printer planning independently from the immutable source layout. No hardware is triggered by these checks.

## Capture settings

“Chụp & đếm ngược” provides a 3/5/10-second countdown selector and compact text inputs for session duration (1–60 minutes) and review duration (1–60 seconds). Defaults are 7 minutes and 7 seconds. Save persists validated values to Operator/capture.json; Cancel restores saved values. Blank, non-integer and out-of-range values cannot be saved.

New customer windows use saved settings. The total clock appears for the final five minutes, or immediately for shorter sessions. Expiry during countdown completes that photo, then advances to favorites and frame selection. SDK capture and session recovery remain separate future work.

## Remaining sections

Color filters currently shows planning cards for Refine, Retouch, Match look and customer presets. Processing, preset publication, authentication, log viewing and a frame library browser are not implemented.

“Cấu hình nháp…” opens the older notes window. It stores draft notes in Operator/settings.json and provides simulated camera capture. These notes do not configure physical devices. “Mở giao diện khách” opens the customer preview.

## Verification

Build on Windows, exercise both paper modes, drag and resize at different display scaling levels, test PNG import, save/open a layout and reopen the customer window after applying it. Confirm that one assignment fills duplicated photo numbers and incomplete slots cannot print. Inspect invalid numeric input, malformed files and discard prompts.

Appending --render-preview followed by an absolute PNG path renders the current WPF window and exits. Rendering does not replace pointer, keyboard or physical printer checks.


### Simplified layout editor

Preset and saved-template thumbnails share one top gallery. A single toolbar contains new template, name, undo, redo, save, and a menu for secondary actions. The left panel exposes photo slots, images, text, and session QR; shape tools remain in the menu.

Images can be inserted temporarily into a selected photo slot to check the crop. Trial images are excluded from saved templates and customer layouts. Image/logo decorations remain persistent. The session QR element is a movable placeholder, saved with the template. It does not yet generate a scannable code or upload files: the delivery service for photos, GIF, timelapse and print-sized images is deferred until after UI/UX work.

Resolution can be set to 300 or 600 DPI per template; pixel dimensions update while physical layout geometry stays unchanged. Inspector coordinate fields remain explicitly labelled at 300 DPI. Layers have a separate always-visible list and count. Opening a builtin preset establishes a clean baseline; subsequent edits still require saving. Saving an existing library entry updates its file.

Editor update: zoom is limited to 75-400%. Tool and inspector panels can be collapsed independently. Portrait/landscape is saved per template, with derived read-only pixel dimensions. Frame import is exposed once under layout settings. Delete removes saved files or hides built-in presets; the final visible template cannot be removed. Existing active customer layouts remain separate from the editor library. Border thickness uses an explicit WPF Thickness binding and appearance fields update while editing.

The delete action is next to Save and asks for confirmation (default No). Side panel edges support dragging to resize and double-click to collapse/reopen. The basic gallery now provides ten strip, portrait, landscape, grid and square-photo compositions; all use 2x6 or 4x6 paper. Photo labels use only slot numbers. Photo-number input, border/shadow inspector, customer-activation and shape creation menu entries are removed from this editor UI. Existing stored appearance data is preserved.

Scrollbars use a 10-unit hit area with a thin neutral thumb, darker on hover/drag; panel resize edges stay subtle until hovered. Preset thumbnails are 78 units high in a 148-unit gallery, with separate space for labels and horizontal scrolling.

QR layers now use the stable label Ma QR (no ordinal). The editor draws a vector QR-style sample motif, not a live delivery QR. Session data offers a date element with dd/MM/yyyy, dd-MM-yyyy or dd.MM.yyyy format; the element type and format persist in the template. Preview currently uses the local date; binding to a frozen real session timestamp remains part of session integration. Explanatory inspector copy has been removed.

Date elements always render dd/mm/yyyy in the editor. PrintLayout.ResolvePrintData takes the explicit print-job timestamp and returns resolved text without changing the template; the future physical printing integration must call this once per job and preserve it for retries. QR resizing is square with an 80-unit (20.32 mm) minimum including the quiet area; scanning still needs verification with the final encoded payload and printer. Aspect controls are shown only for photos/images. Frame import is beside New Template.

Image now opens the PNG/JPEG picker directly and inserts a persistent image layer. Delete removes the selected unlocked element outside text inputs. Each selected element has a mouse rotation handle (rotation stays within page bounds). Inspector title is the selected element name; no selection shows no properties. Clicking empty paper clears selection.

Rotation uses middle-button drag on an unlocked visible element; the top handle is removed. Layer rows expose visibility and lock toggles. Visibility persists in templates and excludes hidden elements from customer preview. Rotation display rounds to one decimal place.

Rotation interaction: wheel over canvas rotates the selected visible unlocked element by 1 degree per notch; Shift uses 0.1 degree. Ctrl-wheel retains zoom. Enter commits the rotation field and retains validation errors for invalid values. The inspector heading reads Da chon or Chua chon; add-tool icons share a centered 20-unit box. Middle-button dragging is removed.

Add-tool content now honors left alignment in the button template. Tool rows use consistent 40-unit height; sidebar scroll areas suppress dotted focus adorners and reserve a small right/bottom inset.

Management UI: overview filters today/month/year/quarter/custom dates and shows session/paper totals. Revenue remains empty behind a vector lock until a locally salted PBKDF2 password is configured and verified; navigation relocks it. Prices persist per size and total copies 2-5. Library reads operator sessions.json and opens each existing download folder. These views currently have no production session/payment/print ingestion or hosted link generation; empty data is shown rather than fabricated metrics. QR can now shrink to 20 layout units (60 px at the inspector 300-DPI scale); real QR scanning remains unverified.

General settings now persist fullscreen, hardware rendering, data/export directories and planned output formats. Only display settings currently affect the customer window; capture/export pipelines are not connected to the configured directories yet. A separate 6-12 digit PIN is hashed with salted PBKDF2; changing it requires the existing PIN. Five failed attempts impose a process-local 30-second cooldown. First-run setup routes to Quyền quản trị (PIN). Màn hình & thương hiệu stores the brand name and an optional PNG/JPEG logo (up to 5 MB, copied into the Operator folder on save); the customer header, welcome title and footer show the logo when set, otherwise the brand name. The same page sets the customer theme for this installation (one activation key = one brand): accent colour (#RRGGBB, must give at least 3:1 contrast with white button text), background colour (at least 4.5:1 against the ink text) and an optional PNG/JPEG background image up to 10 MB shown under a 55% overlay of the background colour. The accent recolours buttons, selections, progress and the welcome glow; the default accent keeps the original Foto Hunter palette. The capture screen keeps its dark background. Customer launch requires a PIN, hides the owner admin window, and adds a subtle upper-right lock button. Normal close, Escape and exiting fullscreen via F11 require PIN and return to admin. The PIN prompt is a touch keypad (digits 0–9, ⌫ deletes one digit, ✓ unlocks once 6 digits are entered, Hủy cancels); a physical keyboard can type digits, Backspace, Enter and Esc. This application gate does not replace Windows kiosk configuration. Event triggers, virtual-keyboard forcing, external payments and live APIs are not implemented. See activation-api.md for the proposed brand licensing service.
# Một cửa sổ quản trị

## Cài đặt in — 29/09/2026

Tất cả tùy chọn Boolean trong PrinterSettingsPanel hiển thị dạng công tắc, vùng chạm cao tối thiểu 44 px; giữ nguyên binding và lưu cấu hình. `PrinterConfiguration.CreateSheetPlan` tạo vị trí ghép theo pixel, `PrintSheetComposer.Compose` nhận ảnh một mẫu đã hoàn thiện và tạo ảnh tờ in. Khi PairAndCut bật với SingleStrip: 600 × 1800 thành 1200 × 1800 ở 300 DPI; 1200 × 3600 thành 2400 × 3600 ở 600 DPI. Dải ngang ghép theo trục Y. Mẫu 4 × 6 sẵn có không bị nhân đôi. Canvas không đổi. Bộ ghép chưa nối vào pipeline in thật; không gửi spooler hoặc bật dao cắt.

Đã kiểm tra 62 unit tests và kiểm tra bitmap WPF phủ kín tờ giấy ở cả hai DPI/hướng. Driver vẫn cần giấy 4 × 6 và chế độ cắt phù hợp, theo https://support.lumasoft.co/en/articles/12831674-printing-2x6-classic-photo-strips . Chưa kiểm chứng máy in vật lý.

Port nhận diện ô ảnh từ https://github.com/phucnguyenAtWork/Photobooth đang chờ mã nguồn có thể truy cập: truy cập Git yêu cầu xác thực, GitHub API trả 404 với quyền hiện có. Chưa chuyển hoặc thay thế thuật toán của repo đó bằng thuật toán tự suy đoán.

Đã bỏ AdminWindow (quản trị nháp cũ) và các nút mở cấu hình nháp. Nút Khởi động giao diện booth ở cuối thanh điều hướng mở MainWindow qua luồng kiểm tra PIN hiện có. Dữ liệu settings.json cũ vẫn giữ nguyên. Khởi động mặc định hoặc `--simulator` đi vào giao diện booth; nếu chưa thiết lập PIN thì mở Quyền quản trị trước. Công cụ mô phỏng workflow nội bộ vẫn được giữ riêng với `--workflow-simulator`, không xuất hiện trong GUI vận hành.

Nhập khung PNG hiện chỉ nhập artwork 2 × 6 hoặc 4 × 6, nhận diện có trong suốt hay không và cho bố trí ô ảnh thủ công. Chưa triển khai tự dò ô từ màu, hút màu, bút đục/vá hay tạo khung hàng loạt từ thư mục.

# Cài đặt chế độ chụp — cập nhật 28/09/2026

Logic mới theo số ảnh: thanh kéo Số ảnh chụp 4–10 (mặc định 6) thay thiết lập thời gian phiên trong UI. `Shots` lưu cùng capture.json, file cũ mặc định 6. Bộ đếm kết thúc sau thời gian xem lại ảnh cuối, không kết thúc theo SessionMinutes cũ; callback trễ không tạo thêm ảnh giả. Khi đủ ảnh, bản xem thử chuyển thẳng sang chọn khung/ghép ảnh với toàn bộ ảnh đã chụp, bỏ bước chọn yêu thích bắt buộc và nút quay lại bước đó. Số ảnh độc lập số ô: đã kiểm tra chụp 6 rồi ghép ảnh 6/2/5/1 vào khung 4 ô. Máy ảnh thật vẫn chưa tích hợp. Các mô tả phiên tính phút và chọn yêu thích trước khung trong tài liệu cũ đã được thay thế bởi luồng này.

Đếm ngược trước ảnh đã đổi sang 1–29 giây, bước 1 giây, thay các mốc 3/5/10 trước đây. Cả thanh kéo, kiểm tra cấu hình và bộ đếm của bản xem thử dùng phạm vi mới.

Điều chỉnh cảm ứng mới nhất: bỏ cột Nhịp chụp và các dòng giải thích tích hợp khỏi GUI; bảng thiết lập dùng toàn bộ vùng còn lại. Thời gian, số khung, khoảng phát, chất lượng và tốc độ slow-motion dùng slider có thanh tô màu, nút tròn 30 px trong vùng kéo 40 × 48 px và giá trị hiển thị bên phải. Có xử lý chạm/kéo trực tiếp, cuộn dọc bên ngoài slider; nút và danh sách chọn cao tối thiểu 44 px. Đếm ngược ảnh vẫn giữ các mốc 3/5/10 giây để tương thích logic hiện có; các slider số nguyên còn lại tăng từng đơn vị, tốc độ dùng 0.25/0.5/1/2. Đã kiểm tra render và thay đổi giá trị/chuyển chế độ trong WPF; chưa kiểm chứng trên màn hình cảm ứng vật lý. Mô tả bố cục ba cột bên dưới là lịch sử trước điều chỉnh này.

Đổi tên mục Chụp & đếm ngược thành Cài đặt chế độ chụp. Thiết kế mới dùng danh sách chế độ có biểu tượng vector bên trái, một bảng thông số ở giữa và bản tóm tắt nhịp chụp bên phải. Nút lưu/hoàn tác nằm cố định dưới cùng; chỉ bảng thông số cuộn. Ảnh tham khảo được dùng để hiểu chức năng, không sao chép tài nguyên hình ảnh của ứng dụng tham khảo.

Có các trang Ảnh chụp, GIF, Boomerang, Video, 360° / Slow-motion, Timelapse, Lời chúc và Ảnh thẻ. Cấu hình chế độ được lưu trong capture.json cùng cấu hình thời gian hiện có. File cũ tự nhận các giá trị mặc định. Cần bật ít nhất một chế độ; kiểm tra số, kích thước, góc xoay và giới hạn dòng thời gian trước khi lưu. Nhập sai không ghi đè file đã lưu; chuyển chế độ khi còn ô sai bị chặn; chuyển mục quản trị vẫn giữ bản nháp. Hoàn tác phục hồi lần lưu gần nhất. DataContext của UserControl giữ nguồn điều hướng cửa sổ.

Các thiết lập có thể chỉnh/lưu: bật chế độ; đếm ngược; số khung và khoảng phát GIF; phát xuôi/ngược; thời lượng ghi; chu kỳ timelapse; thu âm; kích thước, góc xoay, chất lượng; đường dẫn PNG phủ và nhạc MP3. Slow-motion có 1–12 đoạn, thời lượng mỗi đoạn và tốc độ 0.25/0.5/1/2; tóm tắt tính thời lượng phát từ các đoạn. Tệp PNG/MP3 hiện lưu đường dẫn, chưa đóng gói hoặc xác minh giải mã.

Phần đã nối vào bản xem thử khách: đếm ngược mỗi ảnh (3/5/10 giây), thời gian xem lại và giới hạn phiên; áp dụng khi mở lại cửa sổ khách. Ảnh chụp/Ảnh thẻ hiện dùng chung các thời gian này. Những tùy chọn chế độ khác, âm báo, autofocus, tạo thêm GIF và bật/tắt chế độ mới là cấu hình chuẩn bị tích hợp. Chưa có encoder, quay thật, preview video, nhận chuyển động 360°, chọn micro, chuẩn ảnh thẻ, countdown riêng ảnh đầu/ảnh tiếp theo hoặc xử lý lớp phủ/nhạc. Không tuyên bố các trang này đã là chức năng media production.

Kiểm tra: Windows build; 50 unit tests; 4 kịch bản simulator. WPF kiểm tra 8 trang, trạng thái ô nhập sai, giữ bản nháp và chuyển mục quản trị; kiểm tra hình render ở cửa sổ tối thiểu 1080 × 680. Chưa thử camera hoặc phần cứng.

# Cài đặt in — cập nhật 28/09/2026

Mã QR trong khung là lựa chọn của người quản trị. Mặc định thêm ở 0,55 inch và ưu tiên góc dưới bên phải nếu đủ chỗ; có thể kéo lớn hơn. Không thêm QR thì khung không có QR, còn màn hình giao ảnh cho khách đã có vị trí QR lớn trong bản UI mô phỏng. Chưa có liên kết tải ảnh/QR thật nên 0,55 inch không phải cam kết quét được. Khi tích hợp, cần dùng liên kết phiên ngắn, mã QR có độ phức tạp thấp, nền trắng và quiet zone 4 module, xuất theo lưới pixel nét; kiểm tra bản in bằng điện thoại trên đúng máy in/giấy. Nếu không đạt, dùng QR trên màn hình khách và bỏ QR in. Tham chiếu hướng dẫn của DENSO WAVE: https://www.qrcode.com/en/howto/cell.html và https://www.qrcode.com/en/howto/code.html.

Chèn QR/ngày phiên mới chọn chỗ trống trên tờ in theo đơn vị bố cục 100 đơn vị/inch. Không đặt đè lên các ô hoặc lớp đã có; ưu tiên vùng nền ít mực nếu đã nhập khung. Nếu không có vùng đủ cho QR 0,55 inch, từ chối thêm và báo lý do để quản trị điều chỉnh bố cục. Trình xem trước và bản in dùng chung hình học tờ giấy, nên đổi zoom màn hình không đổi tỉ lệ các phần tử trên tờ. Không thể bảo đảm không che mọi họa tiết của ảnh nhập bằng lấy mẫu màu; cần xem lại bố cục trước khi lưu.

Nhập khung và tạo khung từ ảnh dùng chung nhận diện khổ theo tỉ lệ: 1:3 → 2×6 inch, 2:3 → 4×6 inch, nhận ngang/dọc và sai lệch tỉ lệ tối đa 2%. Hiện khổ ở phần Bố cục; kích thước pixel cập nhật theo DPI đang chọn. Tỉ lệ khác báo lỗi thay vì tự đoán 4×6. Một ảnh ghép hai dải trên tờ 4×6 vẫn được nhận diện là tờ 4×6. Đổi khổ/hướng giữ nguyên kích thước vật lý QR, không phóng QR theo tỉ lệ trang.

QR in tùy chọn trong trình bố cục có kích thước tối thiểu 0,55 × 0,55 inch (55 đơn vị bố cục; tương đương 165 px ở 300 DPI, 330 px ở 600 DPI). Giữ vuông khi kéo kích thước và đổi hướng/khổ. Mở mẫu có QR nhỏ hơn sẽ nâng lên kích thước tối thiểu và đưa về trong trang. Dữ liệu phiên mới có hộp 0,8 × 0,2 inch, cỡ chữ 12 đơn vị, vẫn hiện dd/mm/yyyy trong trình chỉnh sửa. Đây là giới hạn thiết kế; khả năng quét cần kiểm chứng bằng QR liên kết thật và bản in thực tế.

Cảnh báo ở thanh trạng thái quản trị dùng `Warn` để hiện popup “Cần kiểm tra”, biểu tượng và nội dung màu đỏ, nút “Đã hiểu”. Áp dụng cho lỗi nhập/quét/lưu/mở bố cục, giới hạn ô, yêu cầu PIN và các lỗi cấu hình qua LayoutEditorViewModel. Trạng thái thành công vẫn hiện ở thanh dưới, không bật popup. Đã kiểm tra WPF mở/đóng cảnh báo và chuyển lại màu trạng thái bình thường sau thông báo thành công.

Lưu/đóng trình bố cục: cập nhật các ô nhập đang chỉnh trước khi lưu hoặc so sánh thay đổi; chỉ kiểm tra lỗi nhập của trình bố cục, không lấy lỗi nháp từ trang Máy in hay trang khác. Mốc đã lưu là chính dữ liệu đã ghi thành công. Nếu lưu khi đóng thất bại, hiện lý do cụ thể. Đã kiểm tra WPF với tệp tạm riêng: Lưu rồi đóng không hỏi lại; đóng mẫu có thay đổi rồi chọn Lưu chỉ hỏi một lần và đóng; giá trị chưa mất focus được lưu; lỗi ở trang Máy in không chặn lưu bố cục.

Tạo khung từ PNG/JPEG bằng nhận diện ô ảnh đã được bổ sung vào Bố cục in. Xem [quy tắc nhận diện, khử trắng và giới hạn](template-slot-detection.md). Đây là luồng riêng với nút nhập overlay PNG thông thường.

Sửa lỗi chuyển mục: `PrinterSettingsPanel` giữ DataContext điều hướng của cửa sổ; cấu hình máy in chỉ được gán cho `SettingsContent` bên trong. Không gán cấu hình riêng lên UserControl vì Visibility của trang đang dùng `IsPrinterSettings` từ cửa sổ. Đã kiểm tra trên WPF việc chọn Máy in rồi chuyển tới từng mục và quay lại; trang ẩn đúng và giữ nội dung đang nhập, kể cả giá trị chưa hợp lệ.

Mục menu Máy in mở trang "Máy in & căn chỉnh" (cập nhật 20/10/2026; mục Căn chỉnh bản in riêng đã bỏ khỏi menu). Cột trái: máy chính/phụ chọn từ danh sách máy in đã cài trong Windows (Dò lại máy in đọc lại danh sách; máy đã lưu nhưng không còn trong Windows vẫn giữ, ghi "không tìm thấy"), nút Thông số mở PrintDialog của máy đó và hiện dấu ✓ khi đã lưu PrintTicket; đổi máy thì bỏ PrintTicket cũ. Bên dưới là tùy chọn in, số tờ và giới hạn dạng ô −/+, nhãn màn hình khách. Cột phải: căn chỉnh cả hai máy cùng lúc (tỉ lệ 50–150%, ngang/dọc ±20 mm, nút đặt lại). Xác nhận PrintDialog chỉ nhận cấu hình, không gọi PrintVisual/PrintDocument; không có nút in thử. Nút Máy in Windows mở Windows Settings. Bấm Lưu cài đặt in để lưu vào printer.json; cấu hình cũ vẫn đọc được.

Tự động in, nút in thêm, in luân phiên hai máy khác nhau, số tờ, ghép hai dải, nhãn và giới hạn đã có UI và lưu cấu hình. Chưa nối những lựa chọn này vào giao dịch, màn hình khách hoặc hàng đợi in thật. Không có nút in thử giả. Ghép dải không tự bật dao cắt của máy; tùy chọn cắt cần được thiết lập/kiểm chứng trong driver khi tích hợp thiết bị.

DNP DS620A có giao tiếp USB 2.0 theo https://www.dnpphoto.com/products/printers/ds620a ; không giả định Bluetooth tích hợp. Máy qua bộ kết nối riêng phải được Windows cung cấp thành hàng đợi in tương thích. Chưa kiểm chứng DS620A thực tế trong tác vụ này.
