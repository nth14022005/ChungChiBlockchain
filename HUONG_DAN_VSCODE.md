# Bước MetaMask / Sepolia — chạy bằng VS Code

Bản này bổ sung kết nối MetaMask, quyền ví trên contract, cấp/thu hồi từng chứng chỉ, đối soát giao dịch và khôi phục giao dịch đã gửi khi đóng trang. Mặc định vẫn là GiaLap để bản đang chạy không bị lỗi vì thiếu RPC/hợp đồng.

## 1. Cập nhật nguyên thư mục

Dừng ứng dụng bằng Ctrl+C trong Terminal VS Code, đóng VS Code đang mở dự án trước khi cập nhật. Giải nén gói ra Downloads (ngoài dự án), chạy CAP_NHAT.cmd và chọn C:\Projects\ChungChiBlockchain. Công cụ giữ storage, cấu hình và Git hiện có; lưu toàn bộ thư mục cũ thành BACKUP. Chỉ thay thư mục sau khi bản mới build thành công.

Công cụ chỉ cập nhật file, không sao lưu hay chạy SQL. Trong SSMS, sao lưu CSDL rồi chạy `database/003_testnet_pending.sql`. Script đã có `USE [ChungChiBlockchain]`, không chạy nhầm master. Bạn đã dùng bản trước thì không cần chạy lại 002 nếu đã thành công. Script 003 thêm chỉ mục chống hai yêu cầu pending trên cùng chứng chỉ, không thêm bảng, không xóa dữ liệu. Nếu báo trùng pending, giữ nguyên dữ liệu và gửi thông báo để đối soát.

## 2. Mở và chạy trong VS Code

- File → Open Folder → chọn `C:\Projects\ChungChiBlockchain`.
- Terminal → New Terminal. Terminal mặc định PowerShell.
- Từ thư mục gốc, chạy:

```powershell
cd .\src\ChungChiBlockchain.Web
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Mở http://localhost:5087. Giữ Terminal này mở trong khi dùng web. Ctrl+C để dừng. Các lần chạy sau chỉ cần lệnh `dotnet run --launch-profile http` khi Terminal đang ở thư mục Web. Có sẵn Terminal → Run Task → `ChungChi: run (http)` nếu mở đúng thư mục gốc.

## 3. Chuẩn bị ví và test ETH

Bạn đã có biểu tượng MetaMask trong Chrome. Mở extension, dùng một tài khoản riêng cho đồ án. Bật hiển thị mạng thử nghiệm và chọn **Sepolia**, Chain ID **11155111**. Không chọn Ethereum Mainnet. Test ETH chỉ dùng trả phí thử nghiệm.

Lấy test ETH từ một faucet trong danh sách chính thức:
https://ethereum.org/developers/docs/networks/#sepolia

Điều kiện faucet có thể khác nhau. Không mua ETH thật để làm bài. Ví triển khai hợp đồng và ví tổ chức ký cấp/thu hồi đều cần test ETH. Có thể dùng cùng một ví cho demo đầu tiên, rồi tách hai ví khi thử phân quyền. Chỉ dùng địa chỉ công khai 0x… để cấu hình; không nhập seed phrase/khóa riêng vào VS Code, web hoặc gửi cho người khác.

## 4. Triển khai CertificateRegistry

1. Mở https://remix.ethereum.org (kiểm tra đúng tên miền).
2. Tạo/import file `CertificateRegistry.sol` từ thư mục `contracts` của dự án.
3. Solidity Compiler: chọn compiler **0.8.20**, compile `CertificateRegistry.sol`. Giữ nguyên mã contract được cung cấp.
4. Deploy & Run Transactions: chọn **Browser Extension / MetaMask**; một số phiên bản giao diện gọi là **Injected Provider - MetaMask**. Không chọn Remix VM cho bước Sepolia.
5. Trong MetaMask kiểm tra mạng Sepolia và địa chỉ tài khoản triển khai.
6. Chọn contract `CertificateRegistry`, bấm Deploy, xem và xác nhận giao dịch trong MetaMask.
7. Đợi xác nhận, sao chép **địa chỉ contract** (0x + 40 ký tự) trong Deployed Contracts. Địa chỉ này khác transaction hash (0x + 64 ký tự).
8. Ví deploy là owner và được cấp quyền issuer ngay trong constructor. Lưu địa chỉ ví owner và địa chỉ contract để dùng ở bước sau.

Bạn tự ký trên máy mình. Gói mã không chứa một contract Sepolia đã deploy sẵn.

## 5. Tạo RPC Sepolia và cấu hình từ Terminal VS Code

Tạo endpoint Ethereum Sepolia HTTPS từ nhà cung cấp RPC bạn sử dụng (ví dụ dashboard Alchemy hoặc Infura). Chọn đúng Ethereum Sepolia. URL có thể chứa API key; chỉ lưu cục bộ, không đưa lên Git hoặc gửi ảnh lộ khóa.

Dừng web bằng Ctrl+C. Terminal vẫn ở `src\ChungChiBlockchain.Web`. Chạy từng lệnh, thay hai giá trị `DAN_...` bằng thông tin của bạn:

```powershell
dotnet user-secrets set "Testnet:RpcUrl" "DAN_URL_RPC_HTTPS_SEPOLIA"
dotnet user-secrets set "Testnet:ContractAddress" "DAN_DIA_CHI_CONTRACT_0x"
dotnet user-secrets set "Testnet:ChainId" "11155111"
dotnet user-secrets set "Testnet:Confirmations" "2"
dotnet user-secrets set "Blockchain:Mode" "Testnet"
dotnet run --launch-profile http
```

Dự án đã có UserSecretsId nên không cần `dotnet user-secrets init`. Các giá trị nằm ngoài mã nguồn và được nạp ở môi trường Development của launch profile http. Tránh chụp màn hình lệnh chứa RPC key. Nếu chưa có contract hoặc RPC, cứ giữ GiaLap và hoàn thành bước 3–4 trước.

## 6. Cấp quyền ví tổ chức

1. Đăng nhập web bằng tài khoản quản trị.
2. Vào Ví cấp chứng chỉ → Thêm ví. Gắn địa chỉ ví dùng cấp chứng chỉ với đúng tổ chức của NV1.
3. Vào **Cấp quyền bằng MetaMask / đồng bộ testnet**.
4. Kết nối MetaMask bằng ví **owner của contract**. Trang hiển thị mạng và địa chỉ contract đang dùng; kiểm tra khớp cấu hình.
5. Nếu dùng ví khác owner làm ví tổ chức: bấm Cấp quyền cạnh đúng địa chỉ, kiểm tra giao dịch trong MetaMask và ký. Chờ thông báo đủ xác nhận.
6. Bấm Đồng bộ và tải lại trang: trạng thái web phải là Hoạt động. Nếu ví tổ chức cũng chính là owner, chỉ cần Đồng bộ (constructor đã cấp quyền).

Cấp/bỏ quyền chỉ do ví owner ký. Máy chủ không giữ khóa ví. Đổi trạng thái web thủ công không thay thế quyền isIssuer trên smart contract; lúc phát hành máy chủ kiểm tra cả hai.

## 7. Phát hành chứng chỉ đầu tiên lên Sepolia

1. Đăng xuất quản trị, đăng nhập NV1 (nhân viên tổ chức cấp).
2. Chuyển MetaMask sang đúng ví tổ chức vừa được cấp quyền.
3. Tạo chứng chỉ nháp MỚI (người học/khóa học phù hợp). Chứng chỉ đã phát hành bằng giả lập không được biến thành chứng chỉ testnet.
4. Trong danh sách Chứng chỉ, bấm **MetaMask / Giao dịch** ở dòng cần cấp.
5. Kết nối MetaMask → kiểm tra địa chỉ ví, Chain ID và contract → bấm Phát hành qua MetaMask.
6. Xác nhận giao dịch trong extension. Trang sẽ hiển thị tx hash và liên kết Sepolia Etherscan.
7. Đợi ít nhất 2 khối rồi tải lại trang (thường thử sau 30–60 giây, có thể lâu hơn tùy mạng). Worker kiểm tra mỗi 10 giây.
8. Khi trạng thái `da_ghi_chuoi`, tải PDF gốc và vào Xác minh. Kết quả cần hợp lệ, không có nhãn giả lập cho chứng chỉ testnet này.

Bản này ký từng chứng chỉ. Lô nháp vẫn tạo được, nhưng trên testnet mỗi chứng chỉ cần giao dịch riêng; chưa dùng issueBatch một giao dịch cho cả lô.

## 8. Thu hồi và kiểm tra

Tại MetaMask / Giao dịch của chứng chỉ đã cấp, dùng đúng ví đã cấp, nhập lý do và bấm Thu hồi qua MetaMask. Kiểm tra/ký giao dịch, đợi đủ xác nhận, tải lại. Trang Xác minh phải báo đã thu hồi.

Contract hiện yêu cầu ví thu hồi vẫn còn quyền isIssuer. Nếu đã bỏ quyền ví cấp, owner cần cấp lại trước khi ví đó thu hồi. Không thể dùng ví khác để thu hồi chứng chỉ của ví ban đầu.

## 9. Khi đóng trang, từ chối ký hoặc giao dịch treo

- Từ chối ký: ý định pending vẫn được giữ. Kết nối lại đúng ví và tiếp tục; không tạo bản nháp khác để né pending.
- Đã gửi nhưng web chưa nhận tx: trang lưu tx hash cục bộ nếu trình duyệt cho phép. Tải lại, dán tx hash từ MetaMask vào ô Đồng bộ giao dịch đã gửi. Không ký thêm giao dịch khi đã có tx.
- Đóng trang sau khi gửi: worker tìm event cấp/thu hồi từ block lúc chuẩn bị yêu cầu, kiểm tra đúng contract/hash/ví/call data rồi đối soát. Cần RPC hỗ trợ eth_getLogs. Không tự coi lỗi RPC là chứng chỉ không hợp lệ.
- Giao dịch revert và đã ghi nhận tx: sau đủ xác nhận, trạng thái failed/loi; khắc phục quyền ví/phí rồi thử lại trên cùng chứng chỉ. Thu hồi revert thì chứng chỉ chưa bị thu hồi.
- Thay thế giao dịch bằng Speed up với cùng dữ liệu: worker có thể tìm event của tx thay thế. Nếu MetaMask Cancel bằng một giao dịch khác, yêu cầu có thể còn pending; giữ tx hash để quản trị đối soát. Bản này chưa có chức năng tự hủy mọi pending hoặc giải quyết tất cả giao dịch dropped.
- RPC lỗi/thiếu hạn mức: giữ nguyên dữ liệu. Kiểm tra endpoint, mạng và Terminal. Không xóa storage hoặc sửa trạng thái SQL để làm xanh kết quả.

## 10. Quay về giả lập và chạy lại

Dừng web, Terminal VS Code ở thư mục Web:

```powershell
dotnet user-secrets set "Blockchain:Mode" "GiaLap"
dotnet run --launch-profile http
```

Chứng chỉ cũ được xác minh theo hợp đồng/mạng của giao dịch cấp đã lưu, không theo chế độ cấp mới hiện tại. Chứng chỉ testnet vẫn cần RPC hoạt động khi xác minh, dù web đang dùng GiaLap cho cấp mới. Chế độ giả lập chỉ thu hồi chứng chỉ đã cấp giả lập.

## Kiểm thử và giới hạn

Xem `docs/KIEM_THU_TESTNET.md`. Đã kiểm thử local EVM và dịch vụ; chưa thực hiện giao dịch Sepolia bằng ví của bạn. Chỉ hỗ trợ Sepolia trong cấu hình sử dụng; localhost chain 1337 là ngoại lệ riêng cho kiểm thử. Không hỗ trợ mainnet. Hai khối xác nhận phục vụ demo, không phải cam kết finality tuyệt đối. Hệ thống đọc lại receipt cấp khi xác minh công khai.

Giữ bản BACKUP đang chạy ổn. CSDL và storage là trạng thái riêng trên máy bạn, không thay thế bằng dữ liệu mẫu.

## Tài liệu chính thức đã đối chiếu

- Ethereum networks/Sepolia: https://ethereum.org/developers/docs/networks/
- Ethereum JSON-RPC: https://ethereum.org/developers/docs/apis/json-rpc/
- MetaMask eth_sendTransaction: https://docs.metamask.io/metamask-connect/evm/reference/json-rpc-api/eth_sendTransaction/
- ethers BrowserProvider/Contract: https://docs.ethers.org/v6/getting-started/
- Remix Deploy & Run: https://remix-ide.readthedocs.io/en/latest/run.html
