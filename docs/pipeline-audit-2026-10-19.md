# Rà soát pipeline FotoAutomat — 19/10/2026

Phạm vi: đối chiếu mã hiện tại, không bao gồm training AI Match Look. Đây là audit kết nối, không phải kiểm thử phần cứng hoặc chứng nhận phát hành. Không thay mã sản phẩm, không sửa AGENTS.md.

## Kết luận

Repo có công cụ quản trị ảnh và nền workflow mô phỏng; chưa có một pipeline phiên khách xuyên suốt. MainWindow dùng CustomerPreviewFlow (bộ nhớ/timer/ảnh mẫu), còn BoothWorkflow + SQLite chỉ được nối trong SimulatorWindow. Không thể suy ra việc chụp/in thật từ UI hoặc từ test simulator đạt.

## Các điểm đã nối và điểm còn đứt

| Pipeline | Có trong mã | Phần còn thiếu để chạy phiên khách thật |
|---|---|---|
| Quản trị → khách chọn màu | MainWindow.FilterChoices đọc Published.Looks, ảnh PublishedPreviews; default filter được cấu hình | Đóng băng PresetId, phiên bản/hash và toàn bộ recipe theo phiên. Hiện khách giữ FilterIndex và thư viện được cập nhật khi Shared session Changed; nguy cơ màu bị đổi nếu thư viện đổi giữa phiên |
| Quản trị giá → thu tiền | BoothManagementStore.SavePrices lưu prices.json | CustomerPreviewFlow.Packages vẫn hard-code 80k/140k/210k/280k; chưa đọc bảng giá quản trị vào đơn hàng |
| Thanh toán → quyền chụp | UI có QR/cash và workflow có ConfirmDemoPayment chống lặp demo | UI Next chuyển khỏi QrPayment/CashPayment, không có xác nhận provider/thiết bị; QR là placeholder. Cần mã đơn, ledger, số tiền, callback xác thực, chống lặp, đối soát giao dịch chưa rõ |
| Camera → live view/capture | ICamera.CaptureAsync, SimulatedCamera tạo BMP 96×64; cấu hình đếm ngược | Chưa có camera adapter thật, stream live view, nhận file gốc, reconnect/timeouts. TimedCapture tăng số ảnh, chưa chờ kết quả thiết bị. Cần CaptureCompleted(photoId/path), không dùng hết timer như bằng chứng chụp thành công |
| Ảnh từng lượt → chọn/ghép ảnh | CustomerPreviewFlow giữ số ảnh/favorites/slots | XAML thumbnails/beauty/slots dùng cùng SelectedColorImage lấy từ preview ảnh mẫu. Cần photoId → đường dẫn gốc/thumbnail/ảnh đã xử lý riêng; slot lấy ảnh theo PhotoId, không theo ảnh hiện đang chọn |
| Chụp → xử lý màu | ColorProcessor, LUT, Curves, HSL, Color Balance đã có; editor chạy ảnh thật | BoothWorkflow.CaptureAsync chỉ lưu path; không gọi renderer màu. Cần job xử lý dùng recipe đã đóng băng, ghi output và trạng thái lỗi/cancel |
| Làm đẹp khách → Retouch | RetouchProcessor có xử lý; ColorPreviewSession áp Retouch trước màu | BeautyLevel chỉ là số trong flow, không nối renderer. Faces/Refine đang gắn SamplePath của editor. Cần phát hiện vùng cho từng ảnh mới và mapping mức beauty sang settings; không dùng tọa độ ảnh mẫu cho người khác |
| Ảnh preview → file cuối | MatchImageReader chuẩn EXIF/profile, giới hạn cạnh dài 960; editor preview có Refine sau màu | Chưa có pipeline final ảnh chụp đầy đủ độ phân giải nối vào phiên. Cần giữ file gốc, nhánh preview nhỏ và final riêng, cùng recipe/không gian màu; kiểm tra phù hợp live view đèn liên tục với ảnh flash |
| Layout → print sheet | LayoutStore lưu mẫu; MainWindow nạp Active; PrintSheetComposer ghép một layout đã render ra sheet đúng DPI | Chưa tìm thấy caller của PrintSheetComposer trong src; cần renderer ảnh khách + crop + artwork + text → singleLayout, rồi Compose → file in. Preview WPF không phải file in đã nghiệm thu |
| Sheet → máy in | PrinterSettingsPanel mở PrintDialog để lưu queue/ticket; IPrinter, SimulatedPrinter | Chưa adapter gửi spool thật, theo dõi job, lỗi giấy/máy, copies/cut, đối soát kết quả không rõ. Tiến độ khách hiện tăng +20 mỗi tick; không phải trạng thái máy in |
| Phiên → QR tải ảnh | Có trang Delivery, trường ShareUrl trong kiểu report | Chưa thấy upload service/outbox/gallery/token; cần URL riêng phiên, retry offline, hết hạn, retention và xóa ảnh theo chính sách. Không dùng QR thanh toán làm QR giao ảnh |
| Phiên → lưu trữ/phục hồi/doanh thu | SQLite simulator ghi chuyển trạng thái, chống trùng receipt, RecoverInterruptedSessions; management đọc sessions.json | MainWindow không dùng SQLite workflow. Chưa có writer nối phiên hoàn tất vào báo cáo doanh thu; cần một nguồn giao dịch chuẩn và cập nhật báo cáo từ đó |
| Phát hành/vận hành | Có CI Windows/Linux và bundle đánh giá; PIN quản trị, lưu cấu hình | Chưa chứng minh installer/ký số/update rollback/migration/giám sát máy đầy đủ; activation-api.md vẫn là đề xuất. PIN không phải Windows kiosk boundary |

## Bằng chứng mã chính

- [MainWindow.xaml.cs](../src/FotoAutomat.Desktop/MainWindow.xaml.cs): FilterChoices, SelectedColorImage, OnColorPreviewChanged, constructor, OnNext. Không dựng BoothWorkflow.
- [MainWindow.xaml](../src/FotoAutomat.Desktop/MainWindow.xaml): [MÃ QR], Capture, Selection, Beauty, Frame cùng binding SelectedColorImage.
- [CustomerPreviewFlow.cs](../src/FotoAutomat.Core/CustomerPreviewFlow.cs): Packages, BeautyLevel, Next, Tick, PreviewPhoto/PreviewSlot.
- [BoothWorkflow.cs](../src/FotoAutomat.Core/BoothWorkflow.cs): CaptureAsync chỉ nhận path; PrintAsync chuyển intent trước IPrinter, có NeedsReview khi kết quả không rõ.
- [SimulatorWindow.xaml.cs](../src/FotoAutomat.Desktop/SimulatorWindow.xaml.cs): nơi nối SqliteBoothStore + SimulatedCamera + SimulatedPrinter.
- [ColorPreviewSession.cs](../src/FotoAutomat.Desktop/ColorPreviewSession.cs): RequestRender, SamplePath gating Retouch/Refine, Published/LivePreviews.
- [MatchImageReader.cs](../src/FotoAutomat.Desktop/MatchImageReader.cs): cạnh dài 960; không được dùng ảnh preview này làm master xuất in.
- [PrintSheetComposer.cs](../src/FotoAutomat.Desktop/PrintSheetComposer.cs), [PrinterSettingsPanel.xaml.cs](../src/FotoAutomat.Desktop/PrinterSettingsPanel.xaml.cs): ghép sheet và cấu hình ticket là hai phần riêng, chưa nối đầu ra khách.
- [BoothManagementStore.cs](../src/FotoAutomat.Infrastructure/BoothManagementStore.cs): Prices/SavePrices, Sessions chỉ đọc report; [activation-api.md](activation-api.md): dịch vụ chưa triển khai.

## Thứ tự nối đề xuất — không phụ thuộc training AI

1. **Phiên khách và cấu hình bất biến:** một sessionId/đơn hàng; snapshot giá, preset, layout, cấu hình capture/print. Nối Customer UI với application service + persistence; giữ thiết bị giả làm adapter thử. Nghiệm thu: đổi cấu hình quản trị không làm đổi phiên đang chạy; restart phục hồi trạng thái và lựa chọn.
2. **Một chuỗi hoàn chỉnh bằng ảnh local:** camera giả trả các JPEG khác nhau → danh mục photoId → thumbnails → chọn ảnh → recipe/beauty → final → layout → print sheet → file output. Nghiệm thu: slot đúng ảnh, ảnh gốc không sửa, bộ lọc không cộng dồn, output đúng DPI/kích thước, job lỗi có thể phục hồi. Đây là bước ưu tiên để phát hiện phần nối thiếu trước khi lắp thiết bị thật.
3. **Camera thật:** SDK cho model được chọn, live view và file ảnh chụp; timeout/reconnect; đo preview/final dưới đèn thực tế. Không tự kích hoạt phần cứng trong audit này.
4. **In thật:** queue/ticket/copies/cut; gửi intent bền vững, gắn spool job id, xử lý Unknown không tự in lại. Nghiệm thu trên DNP được chỉ định.
5. **Thanh toán + doanh thu:** nối bảng giá hiện có, provider/thiết bị tiền mặt, ledger và report; chạy callback lặp/đến trễ/mất mạng. Mô phỏng payment từ bước 1, chỉ bật thu tiền thật khi tích hợp được duyệt và nghiệm thu.
6. **Giao ảnh:** outbox tải lên khi online, URL/token riêng phiên, QR thật, hết hạn/retention. Chụp và in local vẫn chạy khi mất Internet.
7. **Đóng gói/pilot:** installer, migration, update/rollback, activation nếu thuộc phạm vi phát hành, stress test CPU/RAM trên NUC, full-session UI và hardware acceptance.

## Vấn đề Match Look ngoài training

Ngay cả không train thêm, vẫn phải kiểm chứng tính nhất quán recipe giữa ảnh nguồn, phân tích reference, bảo vệ da và `.cube` roundtrip. Các test tổng hợp và thay đổi UI không chứng minh look đúng với ảnh khách thật. Không lấy việc hoàn thiện pipeline để tuyên bố đạt Capture One hoặc >90% giống.

## Kiểm chứng trong lượt audit

- Đọc source và đối chiếu call sites; CodeGraph có kết quả thiếu/cũ nên bổ sung tìm trực tiếp bằng rg.
- Chạy `dotnet test tests/FotoAutomat.Tests --configuration Release --no-restore`: 150 passed, 0 failed, 0 skipped.
- Không chạy camera, printer, thanh toán, upload, UI acceptance hoặc benchmark NUC. Không gọi đây là full end-to-end đã đạt.
- Không chạy lại CI remote hoặc format; brief đã ghi format fail có sẵn. Không tự sửa hàng loạt.
- README/AGENTS có thông tin cũ về màu và CanonCGT; audit ưu tiên hành vi source. Không sửa AGENTS.md theo ràng buộc người dùng.

## Cập nhật 24/10/2026

- Phiên → lưu trữ/phục hồi/doanh thu: đã nối. MainWindow ghi phiên vào `Operator/booth.db` (SqliteCustomerSessionStore) từ lúc thanh toán; shot, lựa chọn, nhật ký và voucher cùng một nguồn; phiên dở khi khởi động chuyển NeedsReview, nhân viên cấp voucher bù. Doanh thu đọc từ booth.db.
- Quản trị giá → thu tiền: bảng giá Khổ nhỏ/Khổ lớn và voucher đã vào đơn hàng của phiên. Thanh toán thật vẫn chưa có.
- Còn mở: dựng file in và in thật, thanh toán thật, giao ảnh/QR.

