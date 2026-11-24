using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.ChungChi;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services.Blockchain;

public sealed record SigningRequest(
    int RequestId,
    int CertificateId,
    string Wallet,
    string Contract,
    long ChainId,
    string Hash,
    string Kind,
    string? TxHash);

public sealed class TestnetWorkflow(
    AppDbContext db,
    EthereumRpcService rpc,
    IBlockchainService active,
    IChungChiService files,
    DemoOperationLock gate)
{
    private IQueryable<GiaoDichBlockchain> Transactions() =>
        db.GiaoDichBlockchains
            .Include(g => g.HopDong)
            .Include(g => g.Vi)
            .Include(g => g.ChungChi)
            .ThenInclude(c => c.NguoiHoc);

    private void Enabled()
    {
        if (active.LaGiaLap)
            throw new InvalidOperationException(
                "Đang dùng giả lập. Cấu hình chế độ Testnet trong VS Code trước.");
    }

    private SigningRequest Request(GiaoDichBlockchain g) =>
        new(
            g.Id,
            g.ChungChiId,
            g.Vi!.DiaChiVi,
            g.HopDong.DiaChiHopDong,
            g.HopDong.ChainId,
            g.ChungChi.HashChungChi!,
            g.Loai,
            g.TxHash);

    public async Task<SigningRequest> PrepareAsync(
        int org,
        int user,
        int certificateId,
        string wallet,
        string kind,
        string? reason,
        CancellationToken ct)
    {
        Enabled();
        wallet = EthereumRpcService.Address(wallet);

        if (kind is not ("cap" or "thu_hoi"))
            throw new InvalidOperationException("Thao tác không hợp lệ.");

        if (kind == "thu_hoi" &&
            (string.IsNullOrWhiteSpace(reason) || reason.Length > 500))
        {
            throw new InvalidOperationException("Nhập lý do thu hồi từ 1 đến 500 ký tự.");
        }

        await gate.Gate.WaitAsync(ct);
        try
        {
            var c = await db.ChungChis
                .Include(x => x.NguoiHoc)
                .SingleOrDefaultAsync(
                    x => x.Id == certificateId && x.NguoiHoc.ToChucId == org, ct)
                ?? throw new InvalidOperationException("Không tìm thấy chứng chỉ của tổ chức.");

            var issuer = await db.ViBlockchains.SingleOrDefaultAsync(
                v => v.ToChucId == org &&
                     v.DiaChiVi == wallet &&
                     v.TrangThai == "hoat_dong", ct)
                ?? throw new InvalidOperationException(
                    "Ví chưa thuộc tổ chức hoặc chưa được quản trị kích hoạt.");

            await rpc.CheckContractAsync(rpc.Config.ContractAddress, ct);

            if (!await rpc.IsIssuerAsync(rpc.Config.ContractAddress, wallet, ct))
                throw new InvalidOperationException(
                    "Ví chưa được cấp quyền isIssuer trên smart contract.");

            var old = await Transactions().SingleOrDefaultAsync(
                g => g.ChungChiId == certificateId && g.TrangThai == "pending", ct);

            if (old is not null)
            {
                if (old.Loai != kind ||
                    old.ViId != issuer.Id ||
                    old.HopDong.ChainId != rpc.Config.ChainId ||
                    !EthereumRpcService.Same(old.HopDong.DiaChiHopDong, rpc.Config.ContractAddress))
                {
                    throw new InvalidOperationException(
                        "Chứng chỉ có yêu cầu khác đang chờ. Dùng đúng ví và cấu hình của yêu cầu đó.");
                }

                return Request(old);
            }

            var issued = await Transactions()
                .Where(g => g.ChungChiId == certificateId &&
                            g.Loai == "cap" &&
                            g.TrangThai == "success")
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync(ct);

            if (kind == "cap")
            {
                if (c.TrangThai is not ("nhap" or "loi") || issued != null)
                    throw new InvalidOperationException(
                        "Chỉ phát hành bản nháp hoặc giao dịch cấp đã thất bại rõ ràng.");

                if (await files.LayFileAsync(org, certificateId, ct) is null)
                    throw new InvalidOperationException(
                        "PDF thiếu hoặc đã bị sửa; không được phát hành.");

                var record = await rpc.TraCuuAsync(c.HashChungChi!, ct);
                if (record.TrangThai != TrangThaiTrenChuoi.KhongTimThay)
                    throw new InvalidOperationException(
                        "Hash đã có trên chuỗi; cần đối soát giao dịch cũ thay vì ký lại.");
            }
            else
            {
                if (c.TrangThai != "da_ghi_chuoi" ||
                    issued is null ||
                    issued.HopDong.ChainId != rpc.Config.ChainId ||
                    !EthereumRpcService.Same(issued.HopDong.DiaChiHopDong, rpc.Config.ContractAddress) ||
                    issued.ViId != issuer.Id)
                {
                    throw new InvalidOperationException(
                        "Phải dùng đúng mạng, hợp đồng và ví đã cấp. Chứng chỉ giả lập không thể thu hồi bằng testnet.");
                }

                var record = await rpc.TraCuuAsync(c.HashChungChi!, ct);
                if (record.TrangThai != TrangThaiTrenChuoi.HopLe ||
                    !EthereumRpcService.Same(record.ViCap, wallet))
                {
                    throw new InvalidOperationException(
                        "Chứng chỉ không còn hợp lệ hoặc ví không phải bên đã cấp trên chuỗi.");
                }
            }

            var address = EthereumRpcService.Address(rpc.Config.ContractAddress);
            var contract = await db.PhienBanHopDongs.SingleOrDefaultAsync(
                h => h.ChainId == rpc.Config.ChainId && h.DiaChiHopDong == address, ct);

            if (contract is null)
            {
                contract = new PhienBanHopDong
                {
                    ChainId = rpc.Config.ChainId,
                    DiaChiHopDong = address,
                    TenMang = rpc.TenMang,
                    PhienBan = "1.0.0",
                    DangSuDung = true,
                    NgayTrienKhai = DateTime.UtcNow
                };
                db.PhienBanHopDongs.Add(contract);
            }

            var g = new GiaoDichBlockchain
            {
                ChungChi = c,
                HopDong = contract,
                Vi = issuer,
                Loai = kind,
                TrangThai = "pending",
                NgayGui = DateTime.UtcNow,
                SoKhoi = Math.Max(0, await rpc.HeadAsync(ct) - 2)
            };
            db.GiaoDichBlockchains.Add(g);

            if (kind == "cap")
            {
                c.TrangThai = "cho_xac_nhan";
            }
            else
            {
                db.ThuHoiChungChis.Add(new ThuHoiChungChi
                {
                    ChungChi = c,
                    GiaoDich = g,
                    NguoiThuHoiId = user,
                    LyDo = reason!.Trim(),
                    NgayThuHoi = DateTime.UtcNow
                });
            }

            if (c.LoPhatHanhId is int batchId)
            {
                var batch = await db.LoPhatHanhs.FindAsync(new object[] { batchId }, ct);
                if (batch != null)
                    batch.TrangThai = "dang_xu_ly";
            }

            await db.SaveChangesAsync(ct);
            return Request(g);
        }
        finally
        {
            gate.Gate.Release();
        }
    }

    public async Task AttachAsync(int org, int requestId, string tx, CancellationToken ct)
    {
        Enabled();
        tx = DinhDangHash.ChuanHoaHoacLoi(tx);

        await gate.Gate.WaitAsync(ct);
        try
        {
            var g = await Transactions().SingleOrDefaultAsync(
                g => g.Id == requestId && g.ChungChi.NguoiHoc.ToChucId == org, ct)
                ?? throw new InvalidOperationException("Không tìm thấy yêu cầu của tổ chức.");

            if (g.HopDong.ChainId != rpc.Config.ChainId || g.Vi is null)
                throw new InvalidOperationException("Sai mạng hoặc yêu cầu không phải testnet.");

            if (g.TxHash != null)
            {
                if (EthereumRpcService.Same(g.TxHash, tx))
                    return;

                throw new InvalidOperationException(
                    "Yêu cầu đã gắn với giao dịch khác. Giữ mã giao dịch thay thế và liên hệ quản trị để đối soát; không ký thêm.");
            }

            if (g.TrangThai != "pending")
                throw new InvalidOperationException("Yêu cầu không còn chờ xử lý.");

            await rpc.ValidateTransactionAsync(
                tx, g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi, g.Loai, g.ChungChi.HashChungChi!, ct);

            if (await db.GiaoDichBlockchains.AnyAsync(x => x.TxHash == tx && x.Id != g.Id, ct))
                throw new InvalidOperationException("Giao dịch đã được dùng cho yêu cầu khác.");

            g.TxHash = tx;
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Gate.Release();
        }
    }

    public async Task ProcessAsync(CancellationToken ct)
    {
        if (active.LaGiaLap)
            return;

        await gate.Gate.WaitAsync(ct);
        try
        {
            var pending = await Transactions()
                .Where(g => g.TrangThai == "pending" && g.HopDong.ChainId == rpc.Config.ChainId)
                .OrderBy(g => g.Id)
                .ToListAsync(ct);

            if (pending.Count == 0)
                return;

            await rpc.CheckNetworkAsync(ct);

            foreach (var g in pending)
            {
                if (g.Vi is null)
                    continue;

                try
                {
                    if (g.TxHash is null)
                    {
                        var found = await rpc.FindEventAsync(
                            g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi, g.Loai,
                            g.ChungChi.HashChungChi!, g.SoKhoi ?? 0, ct);

                        if (found.Tx is null)
                        {
                            g.SoKhoi = found.Next;
                            continue;
                        }

                        await rpc.ValidateTransactionAsync(
                            found.Tx, g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi,
                            g.Loai, g.ChungChi.HashChungChi!, ct);

                        if (await db.GiaoDichBlockchains.AnyAsync(
                                x => x.Id != g.Id && x.TxHash == found.Tx, ct))
                            continue;

                        g.TxHash = found.Tx;
                    }

                    ReceiptResult receipt;
                    try
                    {
                        receipt = await rpc.ConfirmAsync(
                            g.TxHash, g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi,
                            g.Loai, g.ChungChi.HashChungChi!, ct);
                    }
                    catch (ChainUnavailableException)
                    {
                        receipt = new(false, false, null);
                    }

                    // Nếu giao dịch đã được thay thế, tìm lại sự kiện khớp trước khi cập nhật.
                    if (!receipt.Confirmed)
                    {
                        var found = await rpc.FindEventAsync(
                            g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi, g.Loai,
                            g.ChungChi.HashChungChi!, Math.Max(0, (g.SoKhoi ?? 0) - 4), ct);

                        if (found.Tx is null)
                            g.SoKhoi = found.Next;

                        if (found.Tx != null &&
                            !EthereumRpcService.Same(found.Tx, g.TxHash) &&
                            !await db.GiaoDichBlockchains.AnyAsync(
                                x => x.Id != g.Id && x.TxHash == found.Tx, ct))
                        {
                            var replacement = await rpc.ConfirmAsync(
                                found.Tx, g.HopDong.DiaChiHopDong, g.Vi.DiaChiVi,
                                g.Loai, g.ChungChi.HashChungChi!, ct);

                            if (replacement.Confirmed)
                            {
                                g.TxHash = found.Tx;
                                receipt = replacement;
                            }
                        }
                    }

                    if (!receipt.Confirmed)
                        continue;

                    g.SoKhoi = receipt.BlockNumber;
                    g.NgayXacNhan = DateTime.UtcNow;
                    g.TrangThai = receipt.Failed ? "failed" : "success";

                    if (receipt.Failed)
                    {
                        g.ThongBaoLoi = "Giao dịch bị revert trên chuỗi. Có thể thử lại sau khi khắc phục nguyên nhân.";

                        if (g.Loai == "cap")
                        {
                            g.ChungChi.TrangThai = "loi";
                        }
                        else
                        {
                            var revoked = await db.ThuHoiChungChis.SingleOrDefaultAsync(
                                x => x.GiaoDichId == g.Id, ct);
                            if (revoked != null)
                                db.ThuHoiChungChis.Remove(revoked);
                        }
                    }
                    else
                    {
                        g.ChungChi.TrangThai = g.Loai == "cap" ? "da_ghi_chuoi" : "thu_hoi";

                        if (g.Loai == "thu_hoi")
                        {
                            var revoked = await db.ThuHoiChungChis.SingleAsync(
                                x => x.GiaoDichId == g.Id, ct);
                            revoked.NgayThuHoi = DateTime.UtcNow;
                        }
                    }
                }
                catch (TransactionMismatchException ex)
                {
                    g.ThongBaoLoi = ex.Message[..Math.Min(500, ex.Message.Length)];
                }
            }

            await db.SaveChangesAsync(ct);

            foreach (var batchId in pending.Select(g => g.ChungChi.LoPhatHanhId).OfType<int>().Distinct())
            {
                var batch = await db.LoPhatHanhs
                    .Include(x => x.ChungChis)
                    .SingleAsync(x => x.Id == batchId, ct);

                batch.TrangThai = batch.ChungChis.Any(c => c.TrangThai == "loi")
                    ? "loi"
                    : batch.ChungChis.All(c => c.TrangThai is "da_ghi_chuoi" or "thu_hoi")
                        ? "hoan_tat"
                        : "dang_xu_ly";
            }

            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Gate.Release();
        }
    }
}

public sealed class TestnetWorker(
    IServiceScopeFactory scopes,
    ILogger<TestnetWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var workflow = scope.ServiceProvider.GetRequiredService<TestnetWorkflow>();
                await workflow.ProcessAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Chưa đối soát testnet ({Type}): {Message}",
                    ex.GetType().Name,
                    ex is ChainUnavailableException or TransactionMismatchException
                        ? ex.Message
                        : "Kiểm tra CSDL/cấu hình. Yêu cầu vẫn được giữ để thử lại.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }
}