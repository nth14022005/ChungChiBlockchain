# Ghi chú khi công khai mã nguồn

- Dự án sử dụng ASP.NET Core, C#, EF Core, SQL Server, smart contract Solidity. Có các file `database/*.sql` và tài liệu kiểm thử trong `docs/`.
- Đã loại thư mục `bin/`, `obj/`, bản sao lưu UI, file font, báo cáo cá nhân và dữ liệu lưu trữ.
- `appsettings.json` trong bản chia sẻ dùng SQL Server localhost và Windows Authentication làm ví dụ, không chứa thông tin xác thực thật. Cần cấu hình lại theo môi trường.
- Chỉ kích hoạt chế độ blockchain testnet khi đã tự cấu hình RPC và ví thử nghiệm; **không đăng private key hoặc seed phrase**.
- Dự án có tính năng giả lập và các thành phần cho Sepolia, nhưng các thử nghiệm thực tế phải đối chiếu lại; chưa xác minh chạy độc lập sau khi lọc tập tin.
- Một số thư viện giao diện bên thứ ba trong `wwwroot/lib/` đã không đóng gói; nếu giao diện thiếu style hoặc script, cần cài các thư viện từ nguồn chính thức và kiểm tra lại giấy phép.
- Có dùng AI hỗ trợ tạo và chỉnh sửa mã nguồn; không hàm ý toàn bộ code tự viết thủ công.
