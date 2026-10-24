# CanonCGT — checkpoint, dữ liệu và thư viện Digital Camera Looks

Ngày 05/10/2026. Người dùng đã chọn **CanonCGT làm nền phát triển**. Chưa tải/chạy weights, train hoặc thêm thư viện camera vào runtime trong lượt này. Các đề xuất trước đây xem CanonCGT là ứng viên nay được thay bằng quyết định này; vẫn phải kiểm chứng chất lượng và CPU trước phát hành.

## Checkpoint cần thử

Quy trình đầy đủ theo thứ tự, nhánh mặt/cảnh, cách đo phần trăm và hướng dẫn làm tay: [implementation-and-manual-workflow.md](implementation-and-manual-workflow.md).

Upstream liệt kê [E2E.pth và SSL.pth](https://github.com/Jinwon-Ko/CanonCGT/tree/main/pretrained); README mô tả giai đoạn end-to-end và self-supervised, demo dùng SSL.pth. Thử SSL trước và E2E làm đối chứng trên cùng ảnh, không suy checkpoint nào tốt hơn trên booth chỉ từ tên. Tác giả công bố code và weights Apache-2.0. [README](https://github.com/Jinwon-Ko/CanonCGT).

Đây là weights học chuyển màu theo reference, **không phải kho weights Canon/Fujifilm/Sony riêng**. Không cần train một neural network mới cho mỗi look. Lưu look dưới dạng reference/embedding hoặc recipe/LUT tùy đầu ra adapter thực tế; cache gắn với hash reference và model version. Khả năng trích LUT, export ONNX, CPU và ánh xạ thành slider chưa được xác minh ở bước này.

Trước chạy: pin commit upstream, lưu LICENSE/README tại commit đó; tải weights chính thức; xác nhận tải được payload thật thay vì Git LFS pointer/trang HTML; ghi SHA256 và byte size, cấu trúc tensors và checkpoint-to-config compatibility. Dùng môi trường Python tách biệt, kiểm tra loader trước khi nạp checkpoint, không tùy tiện nạp pickle từ mirror. Không đổi PyTorch/CUDA rồi coi sai khác output là chất lượng model. Lưu môi trường, ảnh input và output mẫu để tái lập.

## Danh mục nguồn dữ liệu

| Nguồn | Giá trị | Quyết định lọc ban đầu |
| --- | --- | --- |
| Ảnh booth do mình thu, có quyền train | Đúng camera, da, flash, nền và khách nhiều người | Ưu tiên fine-tune và test; quyền train và quyền công khai ảnh preview ghi riêng |
| RAW + JPEG cùng capture, nhiều style do mình chụp | Nhãn tone camera có nguồn xác nhận | Ưu tiên thư viện camera; giữ firmware, WB, style và renderer version |
| [PST50](https://huggingface.co/datasets/zrgong/PST50) | Benchmark style transfer có target, paired/unpaired, Rec.709 và log | Card ghi CC-BY-4.0; kiểm tra gói tải/attribution trước dùng. Giữ làm benchmark, không đưa vào train. Không coi log là sRGB |
| [PPR10K](https://github.com/csjliang/PPR10K#agreement) | Chân dung theo nhóm, expert targets, human-region masks | Agreement chỉ nghiên cứu phi thương mại, hạn chế cả derived data. Không đưa vào tập train thương mại nếu chưa có quyền phù hợp. Apache của code không thay Agreement của data |
| [MIT-Adobe FiveK](https://data.csail.mit.edu/graphics/fivek/) | Dữ liệu expert edits được CanonCGT dùng | Chờ kiểm tra đầy đủ điều khoản ảnh và preset; không tự duyệt thương mại. Trang nguồn lỗi tải trong lượt này; không tuyên bố đã đọc lại toàn bộ license |
| [raw.pixls.us](https://raw.pixls.us/) | RAW nhiều camera, thông tin EXIF/license từng mẫu | Dùng mẫu đã xác minh quyền để kiểm tra decoder/metadata; không mặc định mọi file CC0. Không đủ để học look: RAW không phải JPEG mang style camera |
| Flickr2K / DIV2K / LSDIR / Food-101 / GLDv2 trong README upstream | Đa dạng nội dung cho self-supervised | Chưa audit riêng trong đợt này; không tải hàng loạt hoặc đánh dấu được train thương mại chỉ vì upstream đã dùng |

Đặc biệt, checkpoint CanonCGT được tác giả cấp Apache nhưng nguồn data train gồm tập có điều kiện riêng. Ghi nhận vấn đề provenance khi phát hành; không tự suy weights bị cấm, cũng không suy Apache giải quyết mọi quyền bên thứ ba. Fine-tune trên data sạch không xóa provenance của checkpoint gốc. Nếu cần chuỗi dữ liệu hoàn toàn kiểm soát được, cân nhắc train từ đầu bằng data được cấp quyền sau khi đánh giá chi phí.

## Thu dữ liệu camera có kiểm soát

Một look gồm **model camera + style/simulation + thông số + ánh sáng/WB**, không chỉ tên hãng. Cùng camera có nhiều tone. Phân biệt profile hiệu chuẩn đầu vào của booth với look mô phỏng đầu ra.

Ưu tiên X-H2, R50, R100 khi thực sự có máy/dữ liệu. Thu RAW + JPEG cùng lần chụp; lưu style, firmware, lens, ISO, exposure, WB mode/Kelvin/shift, dynamic-range options, sharpening/noise settings nếu biết. Không biết để null; EXIF không đủ thì ghi từ cấu hình máy. Chụp chart/gray card và chân dung dưới nhiều ánh sáng; chart chỉ là bổ sung, không thay kiểm tra da và cảnh thật.

Có thể tạo nhiều bản render từ cùng RAW bằng công cụ hãng: [Fujifilm X RAW STUDIO](https://digitalcamera-support-en.fujifilm.com/digitalcameraengpcdetail?aid=000008333) sử dụng engine trong camera qua USB; [Canon DPP](https://asia.canon/en/support/8204716500) hỗ trợ áp Picture Style lên RAW. Kiểm tra model/file compatibility và điều khoản công cụ/preset trước sử dụng. Ghi rõ bản nào JPEG từ máy, bản nào render phần mềm; không gắn nhãn hai cách là giống tuyệt đối chưa đối chiếu.

Ảnh JPEG sưu tầm khác cảnh vẫn dùng làm reference khi có quyền. Chỉ gắn nhãn `reference-inspired`, không gọi là profile camera đã đo. Không lấy tên camera trong EXIF làm bằng chứng chưa chỉnh hậu kỳ. Không tự crawl mạng xã hội hoặc gom preset thương mại để train.

## Quy trình lọc đề xuất

1. **Inventory:** giữ nguyên originals, ghi asset ID, nguồn, SHA256, dung lượng, loại file, rights evidence. Tách quyền commercial training, internal evaluation, redistribution và public preview; unknown đưa vào quarantine.
2. **Decode:** đọc ảnh, EXIF orientation, ICC và bit depth; giữ RAW gốc, tạo working RGB bằng renderer/version cố định. Unknown profile hoặc decode fail chuyển review; không âm thầm gán mọi ảnh sRGB.
3. **Duplicates:** SHA256 loại trùng byte; perceptual hash gợi ý nhóm gần giống để duyệt. Không loại variant khác tone như ảnh trùng vô ích; chúng cùng parent group.
4. **Quality:** ghi độ nét, clipping, resolution, watermark/screenshot, compression, skin/neutral coverage. Đây là cờ review, không loại hết ảnh khó/high ISO/thiếu sáng làm benchmark thiếu thực tế. Tách ảnh hỏng khỏi hard cases.
5. **Labels:** xác nhận camera/style, paired relationship và look family. Chỉ pixel loss khi source-target khớp hình học; reference khác cảnh không cần khớp kích thước/crop. Không kéo giãn ảnh để tạo nhãn giả.
6. **Split:** chia theo connected capture/session/subject groups trước render/crop/augment. Giữ riêng camera/ánh sáng/look family cho bài test tổng quát hóa. Không để cùng RAW hoặc burst lọt qua train/test.
7. **Review:** quản trị kiểm tra contact sheet, da, trung tính và nhãn tone; ghi accepted/quarantine/rejected cùng lý do. Mỗi asset có thể dùng evaluation mà không được train.
8. **Freeze:** xuất manifest có version/hash, thống kê mỗi camera/style/ánh sáng, missing metadata và quyền. Không train khi split và quyền chưa rõ.

Pilot thực dụng: 30 capture độc lập mỗi camera có sẵn, 3 style, ít nhất 3 điều kiện sáng; tăng lên pilot trong training-plan sau khi loader/nhãn đạt. Đây là đề xuất thu thập ban đầu, không đủ chứng minh chất lượng hoặc >90%. Mọi biến thể cùng capture ở cùng split; tách hẳn một số phiên chụp để test.

## Digital Camera Looks trong Adjust — đặc tả chuẩn bị

Một ảnh source được dùng cho **tất cả thumbnail**, mỗi tile áp look khác nhau; thay source thì cập nhật các tile. Không dùng các ảnh khác nhau để khiến filter trông đẹp hơn. Cache thumbnail theo source hash, look version và Normalize policy; chỉ render tile đang hiển thị để giảm tải CPU.

Lọc theo Brand / Camera / Style; xem Before/After, Impact, Save Preset. Metadata kỹ thuật để trong Details, không phủ lên ảnh. Phân biệt `reference-inspired` và `camera-validated`; chỉ cấp nhãn validated khi có kết quả held-out, phạm vi camera đầu vào/ánh sáng và người duyệt. Hiển thị rõ look đang draft hay published. Không hứa tái tạo noise, optics hoặc texture bằng LUT màu.

Model chung + thư viện look độc lập version. Look record cần: ID/name, reference camera/style, input cameras đã test, origin (in-camera/software/reference), reference hashes, recipe/LUT/embedding path, model hash, normalization policy, rights evidence, validation report, preview permission, publication status. Normalize thích ứng mỗi ảnh không được thay bằng LUT cố định mà vẫn gọi là đồng nhất tự động.

## Đầu ra của lượt nghiên cứu này

Đã ghi checkpoint cần thử, registry nguồn ở [source-registry.json](source-registry.json), quy trình lọc và đặc tả xem look camera. Chưa tải dataset/weights, chưa đo checkpoint, chưa thực thi lọc ảnh, chưa thêm UI hoặc train. Bước kỹ thuật kế tiếp: loader checkpoint + inference CPU smoke test, sau đó công cụ inventory/report chạy trên một thư mục ảnh được chỉ định; dùng manifest mẫu trong [training-plan](training-plan.md).
