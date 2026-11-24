import { BrowserProvider, Contract } from './vendor/ethers-6.13.5.min.js';
const root = document.querySelector('#wallet-app, #wallet-admin');
const status = document.querySelector('#wallet-status');
const abi = ['function issue(bytes32)','function revoke(bytes32)','function setIssuer(address,bool)','function owner() view returns (address)','function isIssuer(address) view returns (bool)'];
let injected, provider, signer, account, config, busy = false;
const discovered = [];
window.addEventListener('eip6963:announceProvider', e => {
    if (e.detail?.info?.rdns === 'io.metamask' && !discovered.includes(e.detail.provider)) discovered.push(e.detail.provider);
});
window.dispatchEvent(new Event('eip6963:requestProvider'));
function message(text, error = false) { status.textContent = text; status.className = `alert ${error ? 'alert-warning' : 'alert-info'} mt-3`; }
function buttons() {
    document.querySelectorAll('#issue-wallet,#revoke-wallet,.issuer-action').forEach(b => b.disabled = busy || !signer);
    document.querySelector('#connect-wallet').disabled = busy;
}
function invalidate() {
    signer = null; account = null; buttons();
    message('Ví hoặc mạng đã thay đổi. Bấm Kết nối MetaMask để kiểm tra lại. Nếu giao dịch đã gửi, dùng mã giao dịch để đồng bộ.');
}
async function api(path, body, form = false) {
    const headers = { 'Accept': 'application/json' };
    if (body !== undefined) headers.RequestVerificationToken = root.querySelector('input[name="__RequestVerificationToken"]').value;
    if (body !== undefined && !form) headers['Content-Type'] = 'application/json';
    const response = await fetch(path, { method: body === undefined ? 'GET' : 'POST', headers, body: body === undefined ? undefined : form ? body : JSON.stringify(body) });
    if (!response.headers.get('content-type')?.includes('application/json')) throw new Error('Phiên đăng nhập hết hạn hoặc máy chủ chưa sẵn sàng. Tải lại trang và đăng nhập.');
    const result = await response.json();
    if (!response.ok) throw new Error(result.error || 'Máy chủ chưa xử lý được yêu cầu.');
    return result;
}
function showTx(hash) {
    if (!/^0x[0-9a-f]{64}$/i.test(hash)) return;
    const link = document.querySelector('#transaction-link');
    link.href = `https://sepolia.etherscan.io/tx/${hash}`;
    link.classList.toggle('d-none', config?.chainId !== 11155111);
    const input = document.querySelector('#manual-tx'); if (input) input.value = hash;
}
function saved(id) { try { return localStorage.getItem(`cc-tx-${id}`); } catch { return null; } }
function remember(id, hash) { try { localStorage.setItem(`cc-tx-${id}`, hash); } catch { /* Still submit to server. */ } }
async function checkWallet() {
    if (!signer || !config) throw new Error('Kết nối MetaMask trước.');
    const accounts = await injected.request({ method: 'eth_accounts' });
    const chainId = await injected.request({ method: 'eth_chainId' });
    if (Number(BigInt(chainId)) !== config.chainId || accounts[0]?.toLowerCase() !== account.toLowerCase()) {
        invalidate(); throw new Error('Ví hoặc mạng vừa thay đổi. Kết nối lại trước khi ký.');
    }
}
async function run(action) {
    if (busy) return; busy = true; buttons();
    try { await action(); }
    catch (e) { message((e.code === 4001 || e.code === 'ACTION_REJECTED') ? 'Bạn đã từ chối ký. Yêu cầu được giữ để tiếp tục bằng cùng ví. Chưa có thông báo phát hành thành công.' : (e.shortMessage || e.message || 'Thao tác chưa hoàn tất.'), true); }
    finally { busy = false; buttons(); }
}
document.querySelector('#connect-wallet').addEventListener('click', () => run(async () => {
    config = await api('/Blockchain/Config');
    injected = discovered[0] || window.ethereum?.providers?.find(p => p.isMetaMask) || (window.ethereum?.isMetaMask ? window.ethereum : null);
    if (!injected) throw new Error('Chưa tìm thấy MetaMask extension. Cài từ metamask.io rồi tải lại trang.');
    const expected = `0x${BigInt(config.chainId).toString(16)}`;
    const accounts = await injected.request({ method: 'eth_requestAccounts' });
    if (Number(BigInt(await injected.request({ method: 'eth_chainId' }))) !== config.chainId) {
        try { await injected.request({ method: 'wallet_switchEthereumChain', params: [{ chainId: expected }] }); }
        catch { throw new Error('Hãy bật mạng thử nghiệm trong MetaMask và chọn Sepolia, rồi kết nối lại.'); }
    }
    provider = new BrowserProvider(injected);
    signer = await provider.getSigner(); account = await signer.getAddress();
    if (!accounts.some(a => a.toLowerCase() === account.toLowerCase())) throw new Error('Tài khoản ví thay đổi trong lúc kết nối. Hãy kết nối lại.');
    injected.removeListener?.('accountsChanged', invalidate); injected.removeListener?.('chainChanged', invalidate);
    injected.on?.('accountsChanged', invalidate); injected.on?.('chainChanged', invalidate);
    document.querySelector('#wallet-address').textContent = `Ví đang dùng: ${account}`;
    document.querySelector('#wallet-contract').textContent = `Chain ID: ${config.chainId} · Hợp đồng: ${config.contract}`;
    if (root.id === 'wallet-admin' && account.toLowerCase() !== config.owner.toLowerCase()) {
        signer = null; throw new Error(`Cần kết nối ví owner của hợp đồng: ${config.owner}`);
    }
    message('Đã kết nối đúng mạng. Chọn thao tác và kiểm tra thông tin trong MetaMask trước khi ký.');
}));
async function issueOrRevoke(kind) {
    await checkWallet();
    const reason = document.querySelector('#reason')?.value.trim();
    if (kind === 'thu_hoi' && !reason && root.dataset.kind !== 'thu_hoi') throw new Error('Nhập lý do thu hồi.');
    const request = await api('/Blockchain/Prepare', { certificateId: Number(root.dataset.certificate), wallet: account, kind, reason: reason || (root.dataset.kind === 'thu_hoi' ? 'Tiếp tục yêu cầu thu hồi đã lưu' : null) });
    root.dataset.pending = request.requestId; root.dataset.kind = kind;
    const previous = request.txHash || saved(request.requestId);
    if (previous) {
        showTx(previous); const attached = await api('/Blockchain/Attach', { requestId: request.requestId, txHash: previous }); message(attached.message + ' Tải lại trang sau khoảng 30–60 giây.'); return;
    }
    if (request.wallet.toLowerCase() !== account.toLowerCase() || request.chainId !== config.chainId || request.contract.toLowerCase() !== config.contract.toLowerCase()) throw new Error('Yêu cầu máy chủ không khớp ví/mạng đang chọn.');
    await checkWallet();
    const contract = new Contract(request.contract, abi, signer);
    message('Đang chờ bạn kiểm tra và ký trong MetaMask…');
    const tx = kind === 'cap' ? await contract.issue(request.hash) : await contract.revoke(request.hash);
    showTx(tx.hash); remember(request.requestId, tx.hash);
    message(`Đã gửi giao dịch ${tx.hash}. Đang gửi mã giao dịch về máy chủ…`);
    try { const result = await api('/Blockchain/Attach', { requestId: request.requestId, txHash: tx.hash }); message(result.message + ' Tải lại trang sau khoảng 30–60 giây.'); }
    catch { message(`Giao dịch ĐÃ GỬI: ${tx.hash}. Máy chủ chưa nhận được mã. Không ký lại; tải lại trang và dùng Đồng bộ giao dịch đã gửi. Worker cũng sẽ tìm sự kiện trên chuỗi.`, true); }
}
document.querySelector('#issue-wallet')?.addEventListener('click', () => run(() => issueOrRevoke('cap')));
document.querySelector('#revoke-wallet')?.addEventListener('click', () => run(() => issueOrRevoke('thu_hoi')));
document.querySelector('#attach-tx')?.addEventListener('click', () => run(async () => {
    const hash = document.querySelector('#manual-tx').value.trim() || saved(root.dataset.pending);
    if (!hash) throw new Error('Dán mã giao dịch từ MetaMask vào ô phía trên.');
    const result = await api('/Blockchain/Attach', { requestId: Number(root.dataset.pending), txHash: hash }); showTx(hash); message(result.message);
}));
if (root.dataset.pending && !root.dataset.tx && saved(root.dataset.pending)) document.querySelector('#manual-tx').value = saved(root.dataset.pending);
for (const button of document.querySelectorAll('.issuer-action')) button.addEventListener('click', () => run(async () => {
    await checkWallet();
    const contract = new Contract(config.contract, abi, signer);
    if ((await contract.owner()).toLowerCase() !== account.toLowerCase()) throw new Error('Ví hiện tại không phải owner của hợp đồng.');
    message('Kiểm tra địa chỉ ví cần cấp/bỏ quyền trong MetaMask rồi ký.');
    const tx = await contract.setIssuer(button.dataset.wallet, button.dataset.allowed === 'true'); showTx(tx.hash);
    message(`Đã gửi ${tx.hash}. Đang chờ ${config.confirmations} xác nhận; nếu đóng trang, quay lại và bấm Đồng bộ.`);
    const receipt = await tx.wait(config.confirmations);
    if (!receipt || receipt.status !== 1) throw new Error('Giao dịch cấp quyền chưa thành công.');
    const result = await api('/Blockchain/SyncIssuer', new URLSearchParams({ id: button.dataset.id }), true); message(result.message + ' Tải lại trang để cập nhật bảng.');
}));
for (const button of document.querySelectorAll('.sync-issuer')) button.addEventListener('click', () => run(async () => {
    const result = await api('/Blockchain/SyncIssuer', new URLSearchParams({ id: button.dataset.id }), true); message(result.message + ' Tải lại trang để cập nhật bảng.');
}));
