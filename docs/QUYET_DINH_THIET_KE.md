# Quyết định thiết kế (bản chốt v1)

Đề tài: **Xây dựng hệ thống xác thực chứng chỉ, bằng cấp ứng dụng công nghệ blockchain**
Mục đích tài liệu: chốt các quyết định ngay từ đầu để lúc code không phải đổi hướng, và để hệ thống có thể mở rộng sau này.

> Nguyên tắc: **không thể đảm bảo "không bao giờ sửa", nhưng có thể chốt kỹ những thứ khó sửa
> (quy tắc băm, giao diện contract, cấu trúc CSDL, API) và chừa sẵn "khớp nối" cho những thứ có thể đổi
> (mạng blockchain, nơi lưu file).** Không làm thêm tính năng ngoài danh sách v1.

---

## 1. Phạm vi

### Có trong v1 (bắt buộc làm)
| Nhóm | Chức năng |
|---|---|
| Tài khoản | Đăng nhập, phân quyền 3 vai trò: quản trị, nhân viên tổ chức cấp, bên xác minh |
| Danh mục | Tổ chức, khóa học, loại và mẫu chứng chỉ, người học |
| Cấp chứng chỉ | Tạo PDF kèm mã QR, tính hash, ký giao dịch bằng MetaMask, ghi hash lên blockchain, cấp theo lô |
| Thu hồi | Thu hồi chứng chỉ (ghi lên blockchain, có lý do) |
| Xác minh | Tải PDF, quét QR hoặc nhập mã. Kết quả: hợp lệ / không khớp / đã thu hồi / không tìm thấy. Ai cũng xác minh được, không cần đăng nhập |
| Chia sẻ | Người học tạo liên kết xem và xác minh chứng chỉ, có hạn dùng |
| Hệ thống | Lịch sử xác minh, thông báo, nhật ký hoạt động, thống kê cơ bản |

### Không làm trong v1 (ghi vào "Hướng phát triển" của báo cáo)
Ứng dụng di động, nhiều chuỗi cùng lúc, đăng nhập bằng chữ ký ví, gộp hash theo cây Merkle, gửi email tự động,
chuẩn chứng chỉ số quốc tế (W3C Verifiable Credentials, Open Badges), xóa/ẩn dữ liệu theo yêu cầu người học.

---

## 2. Công nghệ (đã chốt)

| Thành phần | Chọn | Ghi chú |
|---|---|---|
| Web backend | ASP.NET Core MVC (C#) | Lớp: Controllers, Services, Data |
| CSDL | SQL Server + Entity Framework Core | CSDL làm trước (script là nguồn chuẩn), sinh lớp C# từ CSDL |
| Smart contract | Solidity, trình biên dịch 0.8.20 trở lên | Viết và thử trên Remix |
| Mạng blockchain | Testnet (Sepolia nếu còn hoạt động, kiểm tra lại) | Chỉ lưu trong cấu hình/CSDL, không gắn cứng vào code |
| Giao diện | HTML Razor + Bootstrap + JavaScript thuần | Không dùng React |
| Nối ví | `ethers.js` trong trình duyệt + MetaMask | Khóa riêng KHÔNG BAO GIỜ rời trình duyệt/ví |
| Tạo PDF, QR | Thư viện .NET (kiểm tra điều khoản sử dụng khi chọn) | |
| Công cụ | VS Code, SSMS, Git | |

Không dùng: IPFS/Pinata (dữ liệu cá nhân không nên đưa lên nơi không xóa được), Hardhat (làm thêm nếu còn thời gian), mainnet.

---

## 3. Quy tắc bất biến (khó sửa nhất, chốt kỹ)

### 3.1 Quy tắc băm
- `certHash = 0x + SHA-256(toàn bộ byte của file PDF chứng chỉ cuối cùng)`, viết thường, 66 ký tự.
- Mã chứng chỉ (`ma_chung_chi`) và mã QR sinh **trước**, đưa vào PDF, rồi mới băm file PDF hoàn chỉnh. QR chứa mã, không chứa hash, nên không bị vòng lặp.
- Sau khi cấp, **không bao giờ tạo lại PDF** (tạo lại là hash đổi). File gốc lưu bất biến.
- Cột `phien_ban_bam` ghi phiên bản quy tắc (hiện là 1) để sau này đổi cách băm mà không phá chứng chỉ cũ.
- Cột `du_lieu_chung_chi` (JSON) lưu bản chụp dữ liệu tại thời điểm cấp, để sửa thông tin người học sau này không làm lệch chứng chỉ đã cấp.

### 3.2 Mã chứng chỉ
`{MÃ_TỔ_CHỨC}-{NĂM}-{8 ký tự ngẫu nhiên}`, ví dụ `NCTU-2026-K7F3M2QX`. Không dùng số thứ tự (dễ đoán).

### 3.3 Dữ liệu cá nhân
Chỉ lưu trong CSDL. Trên blockchain **chỉ có hash**. Người xác minh thấy tên người học chỉ khi họ có file PDF hoặc mã chứng chỉ.

### 3.4 Smart contract `CertificateRegistry` (giao diện chốt)
| Hàm | Ai gọi | Mô tả |
|---|---|---|
| `issue(bytes32)` | Ví được cấp quyền | Ghi 1 hash |
| `issueBatch(bytes32[])` | Ví được cấp quyền | Ghi nhiều hash (tối đa 100 hash mỗi lần), dùng cho lô phát hành; có hash trùng thì cả lần bị hủy |
| `revoke(bytes32)` | Đúng ví đã cấp | Thu hồi |
| `verify(bytes32)` | Ai cũng gọi | Trả về trạng thái, ví cấp, thời gian |
| `setIssuer(address, bool)` | Quản trị | Thêm/bớt ví cấp |
| `transferOwnership(address)` | Quản trị | Chuyển quyền quản trị (tránh mất khóa là mất hệ thống) |
| `version()` | Ai cũng gọi | Chuỗi phiên bản contract |

Contract không sửa được sau khi triển khai, nên các hàm trên chốt **trước khi** deploy lên testnet. Các lần deploy ghi vào bảng `phien_ban_hop_dong`.

### 3.5 Trạng thái chứng chỉ (`chung_chi.trang_thai`)
`nhap` → `cho_xac_nhan` → `da_ghi_chuoi` → `thu_hoi`. Nhánh lỗi: `loi` (có thể thử ghi lại).

### 3.6 Quy trình cấp chứng chỉ (xử lý chuỗi chậm hơn CSDL)
1. Nhân viên nhập dữ liệu → server tạo mã, PDF, QR, tính hash → lưu bản `nhap`.
2. Trình duyệt nhận hash → MetaMask hiện hộp ký → người dùng xác nhận.
3. Trình duyệt gửi `tx_hash` về server → server ghi giao dịch `pending`, chứng chỉ `cho_xac_nhan`.
4. Khi giao dịch được xác nhận → giao dịch `success`, chứng chỉ `da_ghi_chuoi`.
5. Một tiến trình nền (BackgroundService) định kỳ kiểm tra các giao dịch còn `pending`, phòng khi người dùng đóng trình duyệt giữa chừng.

### 3.7 Kết quả xác minh
| Tình huống | Kết quả lưu (`ket_qua`) |
|---|---|
| Hash có trên chuỗi, chưa thu hồi | `hop_le` |
| Hash có trên chuỗi, đã thu hồi | `da_thu_hoi` |
| Người dùng đưa mã nhưng hash file khác hash đã cấp | `khong_khop` |
| Hash không có trên chuỗi | `khong_tim_thay` |

---

## 4. Kiến trúc: 4 "khớp nối" để dễ thay đổi sau này

Mỗi khớp nối là một interface C#; code còn lại chỉ gọi interface, không gọi trực tiếp công nghệ cụ thể.

| Khớp nối | Triển khai v1 | Có thể thay bằng (tương lai) |
|---|---|---|
| `IBlockchainService` | Đọc/ghi contract trên testnet | Mạng khác, mạng nội bộ của tổ chức; **bản giả lập** để demo dự phòng khi mạng lỗi |
| `IFileStorage` | Thư mục `storage/` trên server | Lưu trữ đám mây |
| `IHashService` | SHA-256 phiên bản 1 | Quy tắc băm mới (nhờ `phien_ban_bam`) |
| API `/api/v1/...` | REST, có Swagger | Ứng dụng di động, hệ thống của doanh nghiệp gọi vào |

API v1 (công khai, không cần đăng nhập): `GET /api/v1/verify/{certHash}`, `GET /api/v1/certificates/{maChungChi}`, `POST /api/v1/verify/file`.
Các trang web (MVC) dùng chính các service này, không viết logic riêng.

---

## 5. Bảo mật và pháp lý (chốt)

- **Server không bao giờ giữ khóa riêng của ví.** Ký giao dịch chỉ xảy ra trong MetaMask của người cấp.
- Mật khẩu băm bằng bộ băm mật khẩu chuẩn của ASP.NET Core; đăng nhập bằng cookie; bắt buộc HTTPS khi triển khai.
- Cấu hình nhạy cảm dùng `dotnet user-secrets`/biến môi trường, không đưa lên Git. `storage/` nằm trong `.gitignore`.
- Tải lên chỉ nhận PDF, giới hạn dung lượng; giới hạn số lần gọi API xác minh để tránh lạm dụng.
- Mọi hành động quan trọng ghi vào `nhat_ky_hoat_dong`.
- Chỉ lưu thông tin do tổ chức cấp/người học cung cấp; phù hợp tinh thần Nghị định 13/2023 về bảo vệ dữ liệu cá nhân.
- Ghi rõ trong báo cáo: hệ thống là **công cụ hỗ trợ xác thực**, không thay thế giá trị pháp lý của văn bằng do cơ quan có thẩm quyền cấp. Blockchain chứng minh "hash này do ví X ghi và chưa bị sửa", không tự chứng minh nội dung là thật.

---

## 6. CSDL

- 19 bảng (script `database/tao_csdl_ChungChiBlockchain.sql`), script là **nguồn chuẩn**.
- Thay đổi sau này viết thành script đánh số: `001_khoi_tao.sql`, `002_...`. Không sửa trực tiếp trong SSMS mà quên ghi lại.
- Bảng `chung_chi` đã có sẵn hai cột `phien_ban_bam` (TINYINT, mặc định 1) và `du_lieu_chung_chi` (JSON, có ràng buộc kiểm tra đúng định dạng JSON).

---

## 7. Cấu trúc thư mục

```
ChungChiBlockchain/
 ┣ database/            script SQL đánh số
 ┣ contracts/           CertificateRegistry.sol
 ┣ docs/                ERD, kiến trúc, báo cáo, nhật ký tuần
 ┣ src/ChungChiBlockchain.Web/
 ┃  ┣ Controllers/  Data/  Models/  Services/  Views/
 ┃  ┣ wwwroot/js/ (web3.js, xacminh.js, abi/)
 ┃  ┣ appsettings.json  Program.cs
 ┣ tests/               kiểm thử dịch vụ băm và xác minh
 ┣ storage/             PDF đã cấp (không đưa lên Git)
 ┣ .gitignore
 ┗ README.md
```

---

## 8. Thứ tự làm (10 tuần, 26/10/2026 - 03/01/2027)

| Tuần | Việc | Ghi chú |
|---|---|---|
| 1 | Chốt đề tài với GVHD, khảo sát hiện trạng, chốt tài liệu này, chạy script SQL, ERD, thử contract trên Remix | |
| 2 | Hoàn thiện contract v1 (3.4), triển khai testnet, khung dự án C# kết nối CSDL, đăng nhập, phân quyền | **Khóa contract tại đây** |
| 3-4 | Danh mục, người học, tạo PDF/QR/hash, lưu trữ file | |
| 5-6 | Cấp chứng chỉ qua MetaMask, trạng thái pending/success, tiến trình nền, cấp theo lô | Giai đoạn khó nhất |
| 7 | Xác minh (tải PDF, QR, nhập mã), thu hồi, API v1 | |
| 8 | Chia sẻ, thông báo, thống kê, **bản giả lập blockchain** cho demo dự phòng | |
| 9 | Kiểm thử, sửa lỗi, bảo mật, quay video demo | **Đóng băng tính năng** |
| 10 | Hoàn thiện báo cáo (viết song song từ tuần 1), chuẩn bị báo cáo với GVHD | Mục tiêu xong code trước 27/12 |

Mốc cứng từ Khoa: nộp báo cáo kèm 1 CD từ 04/01 đến 10/01/2027, nộp trễ là 0 điểm; gặp GVHD ít nhất 4 lần.

---

## 9. Điều kiện để coi là "xong v1"
1. Cấp được chứng chỉ, ghi hash lên testnet, xem được giao dịch trên trình duyệt blockchain.
2. Xác minh đúng cả 4 kết quả ở mục 3.7. Sửa 1 byte trong PDF thì báo không hợp lệ.
3. Thu hồi xong thì xác minh báo "đã thu hồi".
4. Cấp theo lô chạy được.
5. Chạy được ở chế độ giả lập khi không có mạng.
6. README đủ để người khác dựng lại hệ thống từ đầu.
