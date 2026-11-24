# Kiểm thử bản MetaMask — 04/11/2026

## Kết quả thực hiện

Build và chạy bộ kiểm thử thành công bằng .NET SDK 10.0.100. Tổng cộng 65 kiểm tra đạt:

- 12 kiểm tra lõi PDF, hash và chuỗi giả lập.
- 19 kiểm tra nghiệp vụ giả lập: chống trùng, quyền tổ chức, pending, xác minh PDF và thu hồi.
- 27 kiểm tra contract trên EVM cục bộ: đúng chain/contract/ví/operation/hash, xác nhận receipt và event, từ chối receipt thiếu event, giao dịch revert, khôi phục giao dịch mất callback, phát hành và thu hồi, giữ đúng nguồn xác minh khi đổi chế độ.
- 7 kiểm tra HTTP: quản trị không thuộc tổ chức vẫn mở trang quyền ví; cấu hình không lộ RPC; nhân viên không truy cập quản trị; Razor có JavaScript và CSRF; từ chối POST thiếu CSRF; cách ly tổ chức; chặn người chưa đăng nhập.

Contract trong dự án được biên dịch bằng solc 0.8.20 và triển khai trên Ganache chain 1337 để kiểm thử. Đây là EVM cục bộ, không phải kết quả triển khai Sepolia. Tài khoản EVM chỉ tồn tại cho kiểm thử. Xác thực giả bằng header chỉ nằm trong project kiểm thử, không nằm trong ứng dụng web.

## Chạy kiểm thử bằng VS Code

Mở Terminal tại thư mục gốc ChungChiBlockchain. Bộ 31 kiểm tra nền không cần Node:

```powershell
dotnet run --project tests/SmokeTests -- src/ChungChiBlockchain.Web
```

Để chạy đủ 65 kiểm tra, cài Node.js/npm rồi mở hai Terminal. Terminal thứ nhất tại thư mục gốc:

```powershell
cd tests/EvmHarness
npm install
npm start
```

Đợi LOCAL EVM READY. Giữ Terminal này mở. Terminal thứ hai tại thư mục gốc:

```powershell
dotnet run --project tests/SmokeTests -- src/ChungChiBlockchain.Web tests/EvmHarness/fixture.local.json
```

Ctrl+C dừng EVM sau khi kiểm tra. Nếu chạy lại toàn bộ, khởi động lại EVM để có trạng thái mới. Không dùng fixture hoặc tài khoản kiểm thử cho ứng dụng thật.

## Giới hạn và kiểm thử chấp nhận trên máy người dùng

Các kiểm tra dữ liệu dùng EF InMemory; chưa kiểm tra SQL Server thật hoặc chỉ mục SQL 003. Chưa ký thực tế bằng extension MetaMask, chưa triển khai Sepolia trong môi trường kiểm thử này. Script cập nhật Windows kế thừa bản đã chạy thành công trên máy người dùng; thay đổi thông báo mới chưa được chạy lại trên Windows ở đây.

Sau khi sao lưu và cập nhật, làm theo HUONG_DAN_VSCODE.md rồi thử:

1. Chạy SQL 003 trên đúng CSDL; mở ứng dụng trong chế độ GiaLap và kiểm tra chứng chỉ cũ.
2. Triển khai contract Sepolia, cấu hình user-secrets, cấp quyền ví bằng tài khoản chủ hợp đồng.
3. Đăng nhập nhân viên, tạo chứng chỉ mới, kết nối ví đã đăng ký, ký phát hành. Chờ xác nhận rồi kiểm tra tx trên explorer và xác minh PDF.
4. Từ chối ký một lần và thử lại: không sinh ý định trùng.
5. Sau khi đã gửi giao dịch, đóng trang rồi mở lại: worker đối soát hoặc nhập tx hash để tiếp tục.
6. Thử sai ví/sai mạng, tài khoản tổ chức khác và PDF đã sửa: không được báo hợp lệ sai.
7. Thu hồi với lý do, đợi xác nhận rồi xác minh lại.
8. Ngắt RPC: chỉ báo chưa thể xác minh, không kết luận chứng chỉ hợp lệ hay giả vì mất kết nối.

Mỗi chứng chỉ có giao dịch riêng; chưa có issueBatch nguyên tử. Giao dịch bị hủy hoặc không được đưa vào khối có thể cần đối soát thủ công; không tự xóa pending. Hai xác nhận khối là ngưỡng demo, không phải cam kết finality. Chỉ dùng một tiến trình ứng dụng cho bản đồ án này.
