# ChungChiBlockchain — MetaMask / Sepolia và giả lập

Ngày cập nhật: 04/11/2026. Nguồn: Projects.zip người dùng cung cấp.

## Đọc trước khi chạy bản mới

**Làm theo HUONG_DAN_VSCODE.md** để cập nhật nguyên thư mục, chạy SQL 003 và cấu hình MetaMask trong Terminal VS Code. CAP_NHAT.cmd ở ngoài thư mục dự án giữ dữ liệu/cấu hình hiện có và tạo bản sao lưu thư mục. Sao lưu SQL riêng trước khi cập nhật.

Bản mới có kết nối MetaMask, cấp/thu hồi từng chứng chỉ trên Sepolia, quản trị quyền ví và đối soát receipt/event ở máy chủ. Máy chủ không giữ khóa riêng. Giao dịch chưa xác nhận không được coi là phát hành thành công. Mặc định vẫn GiaLap; chỉ bật Testnet khi đã cấu hình RPC và triển khai hợp đồng. Chứng chỉ giả lập cũ giữ nguồn xác minh giả lập.

Xem docs/KIEM_THU_TESTNET.md về phạm vi kiểm thử. Các phần dưới mô tả tính năng nền của bản giả lập; khi cập nhật hãy ưu tiên hướng dẫn VS Code ở trên.

## Đã bổ sung

- PDF tiếng Việt dùng DejaVu Serif nhúng (giấy phép kèm trong Assets/FONT-LICENSE.txt), chuẩn hóa Unicode, co chữ theo độ rộng. Nội dung quá dài dưới cỡ 7 bị từ chối thay vì in chữ không đọc được.
- QR vector chứa URL đầy đủ từ PublicBaseUrl; PDF cũ được giữ nguyên, không tái tạo hoặc băm lại.
- Chống cấp trùng theo người học + khóa học; cho phép cùng người học nhận chứng chỉ khóa khác, và cấp lại sau khi thu hồi.
- Ghi file bằng CreateNew; dọn tệp mới khi tạo lô lỗi. Chỉ mục SQL bổ sung chống hai yêu cầu đồng thời cấp trùng.
- Phát hành từng chứng chỉ hoặc các bản nháp trong lô; mỗi chứng chỉ có một giao dịch giả lập riêng (không phải issueBatch nguyên tử).
- Lưu ý định giao dịch pending trong SQL trước khi ghi chuỗi JSON; worker kiểm tra mỗi 3 giây. Phát lại cùng tx không tạo giao dịch trùng, giúp tiếp tục sau khi dừng ứng dụng.
- Thu hồi có lý do, kiểm tra tổ chức sở hữu; chỉ chuyển trạng thái sau xác nhận giả lập.
- Trang công khai /verify: nhập mã, mở từ QR, hoặc tải PDF tối đa 5 MB; hỗ trợ 4 kết quả, ghi lịch sử khi có hash, giới hạn 30 yêu cầu/phút/IP.
- Giao diện luôn ghi rõ chế độ giả lập. Tra cứu mã không được trình bày như kiểm tra toàn vẹn PDF.

## Cập nhật máy đang có dữ liệu

1. Dừng ứng dụng. Sao lưu CSDL trong SSMS và sao lưu nguyên thư mục dự án, đặc biệt storage/.
2. Giải nén ZIP ra một thư mục tạm. Chép các tệp trong `src/ChungChiBlockchain.Web` vào dự án hiện có, **bỏ qua bin/ và obj/**. Giữ cấu hình kết nối SQL Server của máy bạn nếu khác cấu hình trong gói.
3. Chép `database/002_chong_cap_trung.sql`, `tests/SmokeTests`, README này và `docs/KIEM_THU_CAP_NHAT.md`. Không chép đè storage/ hoặc dữ liệu chuỗi JSON trên máy bạn. Không chép đè .git/.
4. Trong SSMS, chọn đúng CSDL `ChungChiBlockchain`, chạy `database/002_chong_cap_trung.sql`. **Không chạy lại script khởi tạo trên CSDL đang có dữ liệu.** Script 002 sẽ dừng nếu đã có chứng chỉ trùng chưa thu hồi; kiểm tra nghiệp vụ và xử lý dữ liệu trước, không xóa tùy tiện.
5. Kiểm tra appsettings.json:
   - `ConnectionStrings:Default`: SQL Server hiện tại.
   - `PublicBaseUrl`: mặc định `http://localhost:5087`.
   - `Blockchain:Mode`: `GiaLap`.
   - `Blockchain:DoTreXacNhanGiay`: đặt `2` nếu muốn thấy trạng thái chờ rõ hơn.
6. Mở terminal trong `src/ChungChiBlockchain.Web`, chạy:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Dùng .NET SDK 10. Truy cập http://localhost:5087. Nếu dùng profile https, đổi PublicBaseUrl thành https://localhost:7064 trước khi tạo PDF mới.

### Quét QR bằng điện thoại

localhost trên điện thoại là điện thoại, không phải máy chạy ứng dụng. Đặt PublicBaseUrl bằng địa chỉ máy tính trong LAN hoặc tên miền triển khai, cho ứng dụng lắng nghe địa chỉ đó và cấu hình firewall phù hợp. Ví dụ mạng nội bộ: `http://192.168.1.10:5087`. Tạo **chứng chỉ mới** sau khi đổi URL; tuyệt đối không sửa PDF đã phát hành chỉ để đổi QR. Có thể nhập mã thủ công để tra cứu PDF cũ.

## Chạy mới từ đầu

Tạo CSDL bằng `database/tao_csdl_ChungChiBlockchain.sql`, sau đó chạy script 002. Cấu hình kết nối, chạy ứng dụng, tạo tài khoản quản trị đầu tiên tại `/Account/Setup`. Tạo tổ chức hoạt động, nhân viên tổ chức, khóa học, loại/mẫu, người học rồi đăng nhập nhân viên để tạo lô.

## Kịch bản demo

1. Tạo lô gồm 1–3 người học và tải PDF, kiểm tra tiếng Việt/QR.
2. Khi còn nháp: trang xác minh không báo hợp lệ.
3. Bấm Phát hành giả lập, chờ vài giây, tải lại danh sách: `nhap → cho_xac_nhan → da_ghi_chuoi`.
4. Vào `/verify`, nhập mã: bản ghi hợp lệ nhưng chưa kiểm tra PDF.
5. Tải PDF gốc lên: hợp lệ. Tải PDF đã sửa và nhập mã gốc: không khớp. PDF không có bản ghi: không tìm thấy.
6. Thu hồi, nhập lý do, chờ xác nhận: xác minh báo đã thu hồi.
7. Thử nhân viên tổ chức khác truy cập ID chứng chỉ: không được tải, cấp hoặc thu hồi.
8. Chọn lô nhiều người: từng chứng chỉ có giao dịch riêng; một giao dịch lỗi không hoàn tác những giao dịch đã thành công.

## Kiểm thử tự động

Từ thư mục gốc ChungChiBlockchain:

```powershell
dotnet run --project tests/SmokeTests -- src/ChungChiBlockchain.Web
```

Tests dùng EF InMemory, thư mục tạm, không sửa SQL Server hoặc storage/ thật. Đây không thay thế kiểm thử ràng buộc và truy vấn SQL Server. Xem `docs/KIEM_THU_CAP_NHAT.md`.

## Giới hạn còn lại

- Chế độ giả lập dùng một tiến trình ứng dụng và một file JSON. Không chạy nhiều replica cùng storage. MetaMask/testnet đã tích hợp nhưng cần tự triển khai và kiểm thử chấp nhận trên Sepolia.
- Ví cấp trong giả lập là địa chỉ tổng hợp từ ID tổ chức để kiểm tra quyền, không phải ví MetaMask đã đăng ký. Không diễn giải kết quả này là giao dịch blockchain thật.
- Giao dịch `failed` và chứng chỉ `loi` cần xem nhật ký ứng dụng/CSDL để xử lý nguyên nhân. Chưa có nút thử lại hoặc xóa nháp trong giao diện.
- Mẫu chứng chỉ hiện quản lý tên/loại, vẫn dùng một bố cục PDF chuẩn; chưa có trình thiết kế hoặc tải ảnh nền mẫu.
- Chưa có REST API v1, chia sẻ có hạn dùng, giao diện lịch sử xác minh, thông báo, thống kê nâng cao hoặc AI.
- Lịch sử xác minh chỉ được ghi khi có hash (PDF hoặc mã tìm thấy); chưa ghi các lượt nhập mã hoàn toàn không tồn tại.
- Backup/restore SQL và storage phải cùng thời điểm. Không xóa JSON giả lập để “sửa lỗi”, vì sẽ làm mất chứng cứ demo đã phát hành.
- Nếu lỗi quyền hệ thống tệp ngăn dọn file khi tạo lô thất bại, quản trị cần đối chiếu file với bảng tep_chung_chi trước khi dọn thủ công.

## Bước kế tiếp

Chạy kiểm thử chấp nhận trên SQL Server thật, sau đó triển khai hợp đồng và thử ký bằng MetaMask trên Sepolia theo HUONG_DAN_VSCODE.md. Nếu muốn một giao dịch issueBatch chung cho lô, cần migration thiết kế giao dịch–chứng chỉ dạng một-nhiều; chỉ mục tx_hash duy nhất hiện tại không hỗ trợ cách đó trực tiếp.

Tài liệu Tom_tat_Do_an_2.docx và QUYET_DINH_THIET_KE.md là tài liệu thiết kế/lịch sử cũ. Tiến độ thực tế của bản cập nhật này được ghi trong README và báo cáo kiểm thử, không coi mô tả cũ là kết quả kiểm thử mới.
