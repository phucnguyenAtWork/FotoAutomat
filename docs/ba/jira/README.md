# Đưa bộ BA vào Jira SCRUM

Đích đã được người dùng cung cấp: https://phucnguyen31work.atlassian.net, project **SCRUM** (My Software Team), board 1.

Đã xác minh project có Epic, Story, Task và Subtask. Đã tạo đủ 7 Epic, 40 Story theo màn hình và 10 Task năng lực dùng chung ngày 2026-09-24 sau khi người dùng duyệt. Xem [57 ticket đã tạo](published.md). Không import lại CSV để tránh tạo trùng. Không tự gán sprint, assignee, story points hoặc hạn giao hàng.

## Nguồn và bản ngắn

* [Ticket đã xuất bản](published.md): bảng mã BA → Jira; [mapping JSON](published.json).
* [Kế hoạch 3 giai đoạn và 11 sprint](sprint-plan.md): mục tiêu UI/UX → Backend → Test, CI/CD; [mapping sprint/ticket](sprint-published.json).
* [Review backlog](review.md): toàn bộ tiêu đề nhóm theo Epic.
* [backlog.json](backlog.json): nội dung ngắn có cấu trúc cho việc tạo qua API.
* [backlog.csv](backlog.csv): dữ liệu trao đổi/import dự phòng, có ID ngoài và parent ngoài.
* [Danh mục màn hình](../screen-map.md): BA đầy đủ và wireframe nội dung.

Mô tả Jira nêu mục tiêu, phạm vi, ba tiêu chí nghiệm thu và mã BA. Đường dẫn `docs/ba/...` là đường dẫn repository, chưa phải URL public hoặc Confluence page. Không ghi URL GitHub cho file chưa được push.

## Trình tự xuất bản

1. Review danh sách và phạm vi.
2. Tạo Epic trước, lưu mapping mã EP sang key SCRUM thật.
3. Tạo Story/Task với parent là key Epic đã tạo.
4. Ghi mỗi kết quả thành công vào `publish-log.jsonl`, rồi tổng hợp mapping trong `published.json`.
5. Nếu request timeout, tìm lại bằng mã/label trước khi tạo tiếp để tránh trùng.
6. Đọc kiểm chứng parent, tiêu đề và description sau khi tạo.

Backlog CSV không được quảng cáo là nhập trực tiếp không cần mapping. Nếu dùng import, ánh xạ Issue Type, Summary, Description và ID/Parent theo khả năng importer/site; trường tùy chỉnh có thể cần cấu hình. Import tạo hàng loạt thông thường và import có hierarchy không có cùng khả năng.

Tài liệu tham khảo: [Atlassian CSV import](https://support.atlassian.com/jira-cloud-administration/docs/import-data-from-a-csv-file/) và [parent mapping](https://support.atlassian.com/jira/kb/keep-issue-parent-child-mapping-during-csv-import-to-jira-cloud/).

## Jira và BA pages

Có thể lưu BA dài trong Confluence rồi liên kết Story tương ứng. Jira Docs có thể kết nối một Confluence space hoặc page tree. Chưa chọn Confluence space/trang cha nên bộ này hiện là Markdown trong repo và 57 ticket đã tạo trên Jira. [Atlassian hướng dẫn kết nối Docs](https://support.atlassian.com/jira-software-cloud/docs/enable-and-disable-pages).

## Chính sách còn mở

Các mã D-01 đến D-14 không phải sự cố đã giải quyết. Story có quyết định ảnh hưởng tiền/quyền/retention chưa được duyệt cần refinement trước khi triển khai. Bản scaffold simulator hiện có không làm các Story thương mại này tự động thành Done.
