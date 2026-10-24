# v5b bước 1 — tự chọn loại reference (16/10/2026)

Thực hiện bởi Claude (Opus) theo yêu cầu người dùng "test thử v5b".

## Vì sao chỉ là bước 1

v5b đầy đủ cần **model phân đoạn cảnh** (trời, nước, lá, công trình, vật trung tính). Tra cứu giấy phép ngày 16/10:
- Weights SegFormer của NVIDIA chỉ cho nghiên cứu phi thương mại (NVIDIA Source Code License).
- Model huấn luyện trên ADE20K chịu điều khoản của bộ dữ liệu, chưa xác minh được.
- Weights CLIP không có tuyên bố giấy phép rõ ràng.

Theo quy tắc của brief, **không tải model nào** khi người dùng chưa duyệt.

Bước 1 dùng AI **đã có trong app**: Windows FaceDetector (Face priority), để tự giải quyết xung đột đã chứng minh ở [v5a](v5a-content-separation-2026-10-15.md): reference cảnh vật cần v5a, reference cùng loại nội dung cần v4.

## Quy tắc

`ReferenceMatchEngine.IsSceneReference(source, references)`: **ảnh nguồn có mặt người và không reference nào có mặt người** → coi reference là cảnh vật → engine v5a (`reference-anchor-v5a`). Mọi trường hợp khác → v4.

| Ảnh nguồn | Reference | Chọn |
|---|---|---|
| Chân dung | Toàn phong cảnh / kiến trúc | v5a |
| Chân dung | Có ít nhất một chân dung | v4 |
| Không có người (phong cảnh, sản phẩm) | Bất kỳ | v4 |

## Giao diện

Match Look → **···** → ba lựa chọn, mặc định **Reference type: Auto (v5b)**:
- **Auto (v5b):** quyết định riêng cho từng ảnh cần chỉnh. Luôn chạy nhận diện mặt để quyết định, kể cả khi tắt Face priority (vùng mặt sau đó bị bỏ khỏi bước fit nếu Face priority tắt).
- **Same content (v4)** / **Scene (v5a):** ép một engine.

Sau Apply, dòng trạng thái ghi lựa chọn, ví dụ "Auto → Scene (v5a)".

**Thay đổi hành vi mặc định:** trước đây mặc định là v4 trong mọi trường hợp. Giờ chân dung + reference toàn cảnh vật sẽ dùng v5a. Mọi trường hợp khác vẫn như v4.

## Giới hạn

- Nhận diện mặt bỏ sót (mặt nghiêng, nhỏ, che khuất) trong **reference chân dung** sẽ làm nó bị coi là cảnh vật → v5a, tệ hơn cho ca cùng nội dung. Có thể ép "Same content (v4)".
- Phong cảnh → phong cảnh luôn dùng v4, nên reference biển áp lên ảnh núi vẫn có lỗi "màu cảnh thành look".
- Chưa có màu ký ức cho trời, lá, nước. Đó là bước 2, cần model phân đoạn đã duyệt giấy phép.

## Kiểm chứng

143 test đạt (thêm `Auto_reference_type_treats_faceless_references_on_a_portrait_as_scenes`), build 0 cảnh báo, harness đạt. Dữ liệu tổng hợp; **WPF chưa chạy trên màn hình; chưa thử trên ảnh thật.**

## Bước 2 — cần người dùng quyết định

Chọn một hướng:
- (a) tìm model phân đoạn có giấy phép thương mại rõ ràng và dữ liệu huấn luyện cho phép;
- (b) tự huấn luyện model phân đoạn nhỏ trên dữ liệu có quyền dùng (kiến trúc SegFormer/EoMT có code mở, chỉ weights pretrained bị hạn chế);
- (c) dùng phân loại ảnh toàn cục (kiểu CLIP) chỉ để nhận "loại cảnh", khi giấy phép weights được làm rõ.

Mọi lựa chọn phải được duyệt giấy phép trước khi tải.
