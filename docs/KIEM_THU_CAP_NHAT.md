# Kiểm thử bản cập nhật — 02/11/2026

## Đã thực hiện

- Biên dịch ASP.NET Core MVC và Razor bằng .NET SDK 10.0.100: thành công, 0 lỗi, 0 cảnh báo.
- 31 kiểm tra tự động trong tests/SmokeTests: đạt. EF InMemory dùng cho kiểm tra dịch vụ nghiệp vụ/controller; không kết luận truy vấn hoặc ràng buộc SQL Server đã được kiểm chứng.
- Render và xem trực tiếp hai PDF (nội dung thông thường và tên khóa học dài 80 ký tự): không tràn khung.
- Trích xuất chữ bằng PyMuPDF: giữ đúng “Đặng Thị Hồng Ánh”.
- Giải mã QR của cả hai PDF đã render bằng ZXing: đúng URL được truyền vào.

## Nhóm kiểm tra tự động

1. PDF đúng header, cùng dữ liệu cùng byte, Unicode tổ hợp/tách rời cùng kết quả.
2. Sửa một byte làm đổi SHA-256; không được ghi đè PDF.
3. Giả lập cấp/tra cứu, cấp trùng bị chặn, sai ví không thu hồi được, phát lại cùng yêu cầu không tạo thêm, dữ liệu thu hồi tồn tại sau khởi động lại.
4. Tạo nháp thực qua service; chặn trùng cùng khóa, cho phép khóa khác, chặn tải PDF của tổ chức khác.
5. Chặn phát hành/thu hồi chéo tổ chức; bấm cấp lặp không tạo ý định trùng.
6. Chứng chỉ pending chưa hợp lệ, worker xác nhận khi đủ thời gian.
7. Tra cứu mã phân biệt kiểm tra tệp; PDF gốc hợp lệ; PDF sửa kèm mã không khớp; PDF sửa không mã không tìm thấy.
8. Thu hồi bắt buộc lý do, worker hoàn tất, xác minh thấy thu hồi, cho phép tạo lại chứng chỉ sau thu hồi.

## Cần thực hiện trên máy người dùng

- Sao lưu; chạy migration 002 trên đúng SQL Server. Kiểm tra chỉ mục duy nhất và hai yêu cầu tạo nháp đồng thời.
- Kiểm tra đăng nhập, phân quyền bằng cookie và chống giả mạo biểu mẫu qua trình duyệt thật.
- Demo toàn bộ hướng dẫn README với SQL Server.
- Tắt ứng dụng giữa lúc pending, mở lại và kiểm tra tự tiếp tục.
- Bấm phát hành lô nhiều người, kiểm tra trạng thái từng chứng chỉ/lô.
- Thử upload >5 MB, tệp không phải PDF, vượt giới hạn truy cập.
- Quét QR từ điện thoại khi PublicBaseUrl trỏ đúng máy chủ.

Chưa thực hiện kết nối SQL Server thật, kiểm thử trình duyệt đầy đủ, kiểm thử tải, giao dịch mạng blockchain thật hoặc kiểm toán bảo mật.
