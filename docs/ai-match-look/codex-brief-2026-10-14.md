# Brief cho Codex — Match Look (cập nhật 15/10/2026)

Viết bởi Claude (Opus) theo yêu cầu người dùng. Đọc toàn bộ file này trước khi làm bất cứ việc gì về Match Look. Bản này thay thế bản 14/10.

## 1. Match Look là gì — định nghĩa của người dùng

Match Look trong FotoAutomat làm **như Match Look của Capture One** (và Color Match của Photoshop):

1. Quản trị đưa vào **1–8 ảnh reference** cùng một look. Reference có thể là **bất cứ thứ gì**: chân dung, nhà thờ, biển, núi, trời.
2. Match Look **trích xuất thông số chỉnh sửa**: một `ColorRecipe` gồm Exposure, WB/Tint, Contrast, Highlights/Shadows/Whites/Blacks, Curves, HSL, Color Balance shadow/mid/highlight. Thông số nằm trong các nhóm của Capture One: Normalize · Light & Contrast · Color Adjust · Color Tone. Người dùng **thấy và chỉnh tay được** từng thông số, có Impact và "Apply as new Layer".
3. Thông số được áp lên ảnh gốc (thường là chân dung ở booth), cho ra **ảnh đã chỉnh**.
4. Tính năng riêng: **nướng thông số đã tinh chỉnh thành `.cube`**. LUT tạo **từ** thông số bằng phép tính xác định. **Không AI nào sinh LUT trực tiếp.**

### Look là một bộ quy tắc, không gắn với vật thể

Một look là các quy tắc **không phụ thuộc nội dung**:
- **theo độ sáng:** tone curve, điểm đen/trắng, màu vùng tối / vùng giữa / vùng sáng (Color Balance);
- **theo màu:** HSL từng dải (ví dụ xanh lá giảm bão hòa, xanh dương ngả teal);
- **toàn ảnh:** ấm/lạnh, tương phản.

Vì vậy look học từ ảnh nhà thờ vẫn áp được lên chân dung, như một preset Lightroom. **Không cần biết "áp vào mặt hay vào đâu" trên ảnh gốc**: thông số tự áp theo độ sáng và theo màu.

### Vai trò của AI

- **Trên ảnh reference:** tách **look** khỏi **nội dung**. Biển xanh, đá vàng là màu thật của cảnh (không được mang sang). Mây trắng bị ám cam, tường xám ngả teal là look (phải mang sang). Cách làm là **màu ký ức** (memory colors): nhận ra vật có màu tự nhiên đã biết (vật trung tính, trời, mây, lá, da), độ lệch so với màu tự nhiên chính là phần do người chỉnh. Đây là ý "Shazam": nhận đặc trưng của look, bỏ qua nội dung.
- **Trên ảnh gốc:** chỉ **bảo vệ da** theo phương án A.
- Ý mượn từ CanonCGT: **không gian trung tính** (≈ nhóm Normalize) và **vector phong cách** (dấu vân tay look). **Không** mượn phần sinh LUT.

Quyết định đã chốt:
- **Da:** phương án **A**: không look nào được làm da xấu đi.
- **Booth:** khóa Exposure/WB/flash, đèn liên tục + flash.
- **Máy mục tiêu:** NUC i7-1360P / 16 GB, chỉ CPU.

## 2. Đã sai ở đâu (12–13/10)

- CanonCGT (model **ảnh → LUT**) được dùng làm đường chính. Bước 2 và bước 4 bị gộp: AI sinh LUT, thông số bị bỏ qua.
- Chuỗi thí nghiệm tối ưu "LUT của CanonCGT khớp target bao nhiêu", mỗi bước sửa lỗi của bước trước. **Không bước nào hỏi lại: có ra thông số chỉnh được không?** Cả Codex và Opus đều tham gia, và Opus đề xuất nhiều bước trong đó.

## 3. Opus đã làm gì (14–15/10)

**Trong app (C#, có test).** Chi tiết: `docs/reference-match.md` mục v4, `docs/lut-export.md`.
- **Xuất `.cube` từ thông số:** `RecipeLutBaker` + `CubeLutFile` + nút Export .cube. Lưới 33³ lệch trung bình 0,40/255 (tối đa 6). Công cụ phụ thuộc vị trí bị loại và được báo trên UI.
- **Color Balance 3 vùng**, fit trước Curves R/G/B.
- **Look-gram** (màu theo 8 dải sáng): phân biệt được tách tông và bản đảo ngược.
- **Nhiều reference** (1–8, trọng số bằng nhau), UI chọn/thả nhiều ảnh.
- **Bảo vệ da theo phương án A:** khóa màu YCbCr + chroma ≥ 30, phạt mạnh riêng trên pixel da. Reference rực rỡ: da +0,02 bão hòa / 4,5° hue (luật cũ: +0,37 / 20,6°). Vùng không phải da được nhuộm màu theo look.
- Engine `reference-chroma-v4` / `reference-region-v4`. Bỏ qua bánh xe màu trung tính trong `AdvancedColorProcessor`.

**Giới hạn đã biết của v4 (15/10):** reference mà **nội dung chiếm ưu thế màu** bị hiểu nhầm là look. Cảnh biển không chỉnh màu đẩy vùng trung tính của chân dung lệch **+92** về phía xanh. Test `Default_mode_is_v4_and_still_has_the_known_scene_content_weakness` ghi nhận lỗi này; khi sửa được thì đổi test có chủ đích.

**v5a (tùy chọn thử nghiệm, mặc định tắt: menu ··· → Scene reference (v5a), `MatchOptions.SceneReference`):** điểm neo trung tính (chroma ≤ 80) + quantile chỉ học tone. Kết quả: cảnh biển lệch **−0,6**, look ấm vẫn truyền được, tách tông và da vẫn đạt; nhưng **trượt 3 test reference cùng loại nội dung**, vì không còn học bão hòa và màu toàn ảnh. Đây là xung đột thiết kế: phải **biết nội dung reference** mới chọn được cách học → v5b. Chi tiết: `docs/ai-match-look/v5a-content-separation-2026-10-15.md`; patch: `.local/archive/v5a-attempt-2026-10-15.patch`.

**Đã xóa (người dùng yêu cầu):** 71 script CanonCGT/LUT và `tools/LangSmithJudge`. Bản sao: `.local/archive/matchlook-canoncgt-research-2026-10-14.zip`. Script còn lại: `tools/MatchLookData/README.md`.

Trạng thái kiểm chứng: 137 test đạt, build 0 cảnh báo, harness 4/4, MatchBenchmark 3/3. **Tất cả trên dữ liệu tổng hợp; WPF chưa chạy trên màn hình.**

## 4. Quy tắc để không lệch hướng

1. Mọi thay đổi phải trả lời được: **"Người dùng có nhận được thông số chỉnh tay được, giống Capture One, không?"** Không trả lời được thì dừng và hỏi người dùng.
2. LUT chỉ sinh từ `ColorRecipe` qua `RecipeLutBaker`. Không thêm model sinh LUT.
3. Mọi tối ưu fit qua `ColorProcessor`. Không tạo pipeline màu song song (kể cả bằng Python).
4. AI dùng để **phân tích reference** (look hay nội dung) và **bảo vệ da**, không dùng để áp màu theo vị trí. Kết quả cuối vẫn là thông số toàn cục, nướng được thành `.cube`. Mask theo vị trí chỉ khi người dùng chủ động tạo layer.
5. Da theo phương án A: mọi thay đổi loss/thông số phải giữ test da đạt.
6. Đăng ký hằng số và tiêu chí **trước** khi chạy. Không chỉnh heuristic lặp lại trên vài test tổng hợp: một lần thử có nguyên tắc, không đạt thì dừng và ghi lại. Giữ 30 ảnh test cho nghiệm thu cuối.
7. Không xóa file và không sửa `AGENTS.md` khi chưa được người dùng duyệt.

## 5. Việc cần làm — theo thứ tự

1. **Kiểm tra WPF trên màn hình** (Adjust → Match Look):
   - nhiều reference, "+N";
   - Apply, Impact, Apply as new Layer với look có Color Balance;
   - Export .cube rồi nhập lại bằng nhập preset `.cube` của app, so ảnh;
   - thông báo "not in LUT".

   Sửa lỗi giao diện nếu có.
2. **Ảnh thật:**
   - cặp `DSCF9358.JPG` / `test.jpg` (so với số v2/v3 trong `docs/reference-match.md`, và **nhìn da**);
   - **thêm reference phong cảnh** (biển, trời, nhà thờ) áp lên chân dung, **so v4 (tắt) với v5a (bật Scene reference)** trên cùng ảnh: lệch màu vùng trung tính, da, và nhìn bằng mắt. Ghi trường hợp nào v5a tốt hơn hoặc tệ hơn; đây là dữ liệu để thiết kế v5b.

   Không đưa ảnh khách vào repo.
3. **Đo thời gian fit** với ảnh 960 px: 1 và 3 reference, có/không Face priority. Ghi rõ chưa phải số trên NUC.
4. **v5b — AI phân đoạn reference** (phần "AI-assisted" chính). **Bước 1 đã làm (16/10):** tự chọn v4/v5a bằng nhận diện mặt, menu Reference type mặc định Auto. Xem `docs/ai-match-look/v5b-auto-reference-type-2026-10-16.md`. Việc còn lại là bước 2. Viết protocol trước:
   - **chọn model phân đoạn cảnh** chạy CPU (trời, nước, lá, công trình, người/da, vật trung tính). **Kiểm tra giấy phép thương mại trước khi chọn.** Ví dụ: SegFormer của NVIDIA có giấy phép riêng, cần đọc kỹ; ưu tiên Apache/MIT/BSD. Đưa danh sách ứng viên và giấy phép cho người dùng duyệt **trước khi tải hoặc tích hợp**;
   - dùng phân đoạn để: (a) nhận vật có màu ký ức → ước lượng look; (b) quyết định reference **khác** hay **cùng loại** nội dung với ảnh nguồn → chọn trọng số (tham khảo patch v5a cho trường hợp khác nội dung);
   - tiêu chí: **tự** chọn đúng cách học cho reference cảnh vật và reference cùng loại nội dung, không cần người dùng bật tay Scene reference; mọi test hiện có vẫn đạt (test ghi nhận lỗi v4 được đổi có chủ đích); thêm test với reference có trời/lá;
   - model chạy trong Infrastructure (ONNX Runtime hoặc tương đương), Core chỉ nhận vùng/nhãn như `MatchPhoto.Regions` hiện nay.
5. **Bộ dự đoán thông số** ("dấu vân tay" → `ColorRecipe` ban đầu tức thì, optimizer tinh chỉnh tiếp). **Protocol chi tiết: `docs/ai-match-look/param-predictor-protocol-2026-10-16.md`**, gồm bằng chứng, giấy phép từng công trình và 5 quyết định cần người dùng duyệt trước P0:
   - dataset render bằng một tool .NET dùng `ColorProcessor` (không viết lại pipeline màu bằng Python);
   - recipe ngẫu nhiên theo phân bố thực tế;
   - đo trên nội dung chưa thấy.

   Làm sau v5b, hoặc song song nếu người dùng đồng ý. Báo người dùng trước khi train.
6. **Cập nhật `docs/ai-match-look/phase-status.md`**: hướng thông số là trạng thái hiện tại; ghi giới hạn v4 và v5a; CanonCGT là lịch sử.
7. **Đề xuất** (không tự sửa) câu thay thế cho dòng CanonCGT ở đầu `AGENTS.md`.

Ngoài phạm vi: `dotnet format --verify-no-changes` đang fail trên nhiều file có sẵn từ trước. Báo người dùng, không tự sửa hàng loạt.

## 6. Báo cáo cho người dùng

Bằng tiếng Việt, ngắn gọn:
- việc đã làm theo từng mục 1–7;
- số liệu và **cách đo**;
- đã kiểm chứng / chưa kiểm chứng, tách riêng;
- lỗi tìm thấy và đã sửa;
- câu hỏi cần quyết định (đặc biệt: chọn model phân đoạn và giấy phép).

Không gọi là "đạt" khi chưa chạy thật. Không gọi kết quả tổng hợp là kết quả ảnh booth.
