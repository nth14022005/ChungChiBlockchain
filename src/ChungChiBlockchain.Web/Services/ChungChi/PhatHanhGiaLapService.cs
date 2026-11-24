using System.Security.Cryptography;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.Blockchain;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services.ChungChi;

// Bản demo một tiến trình. Không dùng nhiều replica cùng một file JSON.
public sealed class DemoOperationLock { public SemaphoreSlim Gate { get; } = new(1,1); }
public sealed class PhatHanhGiaLapService(AppDbContext db, IBlockchainService chain,
    IChungChiService certificates, DemoOperationLock operationLock, ILogger<PhatHanhGiaLapService> logger)
{
    public async Task<string> GuiAsync(int tc, int user, int? id, int? loId, string? lyDo, CancellationToken ct)
    {
        if(chain is not GiaLapBlockchainService) return "Chức năng này chỉ dành cho chế độ giả lập.";
        await operationLock.Gate.WaitAsync(ct);
        try
        {
            bool revoke = lyDo is not null;
            if(revoke && (string.IsNullOrWhiteSpace(lyDo) || lyDo.Length > 500)) return "Nhập lý do thu hồi từ 1 đến 500 ký tự.";
            var query=db.ChungChis.Include(c=>c.NguoiHoc).Include(c=>c.GiaoDichBlockchains).ThenInclude(g=>g.HopDong).Include(c=>c.ThuHoiChungChi)
                .Where(c=>c.NguoiHoc.ToChucId==tc);
            if(id.HasValue) query=query.Where(c=>c.Id==id.Value);
            else if(loId.HasValue && !revoke) query=query.Where(c=>c.LoPhatHanhId==loId.Value && c.TrangThai=="nhap");
            else return "Yêu cầu không hợp lệ.";
            var list=await query.ToListAsync(ct);
            if(list.Count==0 || list.Count>100) return "Không tìm thấy chứng chỉ phù hợp (tối đa 100 mỗi lần).";
            foreach(var c in list)
            {
                if(revoke && c.GiaoDichBlockchains.Any(g=>g.Loai=="cap"&&g.TrangThai=="success"&&g.HopDong.ChainId!=-1)) return "Chứng chỉ này thuộc testnet. Chuyển đúng chế độ và dùng ví đã cấp để thu hồi.";
                if(c.GiaoDichBlockchains.Any(g=>g.TrangThai=="pending")) return "Chứng chỉ đang chờ xử lý. Hãy tải lại danh sách sau vài giây.";
                if(revoke ? c.TrangThai!="da_ghi_chuoi" || c.ThuHoiChungChi!=null : c.TrangThai!="nhap") return "Trạng thái chứng chỉ không cho phép thao tác này.";
                if(!revoke && await certificates.LayFileAsync(tc,c.Id,ct) is null) return "PDF không tồn tại hoặc đã bị sửa; không thể phát hành.";
            }
            var contract=await db.PhienBanHopDongs.FirstOrDefaultAsync(h=>h.ChainId==-1 && h.DiaChiHopDong==GiaLapBlockchainService.ViGiaLap,ct);
            if(contract is null) { contract=new PhienBanHopDong{ChainId=-1,DiaChiHopDong=GiaLapBlockchainService.ViGiaLap,TenMang="Giả lập nội bộ",PhienBan="demo-1",DangSuDung=true,NgayTrienKhai=DateTime.UtcNow}; db.PhienBanHopDongs.Add(contract); }
            foreach(var c in list)
            {
                var g=new GiaoDichBlockchain {ChungChi=c,HopDong=contract,Loai=revoke?"thu_hoi":"cap",TxHash="0x"+Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)),TrangThai="pending",NgayGui=DateTime.UtcNow};
                db.GiaoDichBlockchains.Add(g);
                if(revoke) db.ThuHoiChungChis.Add(new ThuHoiChungChi {ChungChi=c,GiaoDich=g,NguoiThuHoiId=user,LyDo=lyDo!.Trim(),NgayThuHoi=DateTime.UtcNow});
                else c.TrangThai="cho_xac_nhan";
                if(c.LoPhatHanhId is int batchId) { var batch=await db.LoPhatHanhs.FindAsync(new object[]{batchId},ct);if(batch!=null)batch.TrangThai="dang_xu_ly"; }
            }
            // Lưu ý định bền vững TRƯỚC khi ghi JSON. Worker phát lại cùng tx nếu tiến trình dừng.
            await db.SaveChangesAsync(ct);
            return $"Đã tiếp nhận {(revoke?"thu hồi":"phát hành")} {list.Count} chứng chỉ giả lập. Tải lại sau vài giây để xem kết quả.";
        }
        finally { operationLock.Gate.Release(); }
    }

    public async Task XuLyAsync(CancellationToken ct)
    {
        if(chain is not GiaLapBlockchainService fake) return;
        await operationLock.Gate.WaitAsync(ct);
        try
        {
            var pending=await db.GiaoDichBlockchains.Include(g=>g.HopDong).Include(g=>g.ChungChi).ThenInclude(c=>c.NguoiHoc)
                .Where(g=>g.TrangThai=="pending" && g.HopDong.ChainId==-1).OrderBy(g=>g.Id).Take(200).ToListAsync(ct);
            foreach(var g in pending)
            {
                try
                {
                    var c=g.ChungChi;
                    var issuer="0x"+c.NguoiHoc.ToChucId.ToString("x40");
                    if(g.Loai=="cap") await fake.GhiCapAsync(c.HashChungChi!,issuer,g.TxHash);
                    else await fake.GhiThuHoiAsync(c.HashChungChi!,issuer,g.TxHash);
                    var receipt=await fake.LayTrangThaiGiaoDichAsync(g.TxHash!,ct);
                    if(receipt.TrangThai!=TrangThaiGiaoDich.ThanhCong) continue;
                    var record=await fake.TraCuuAsync(c.HashChungChi!,ct);
                    if(record.ViCap!=issuer || (g.Loai=="cap" ? record.TrangThai!=TrangThaiTrenChuoi.HopLe : record.TrangThai!=TrangThaiTrenChuoi.DaThuHoi))
                        throw new InvalidOperationException("Bản ghi giả lập không khớp giao dịch.");
                    g.TrangThai="success";g.SoKhoi=receipt.SoKhoi;g.NgayXacNhan=DateTime.UtcNow;
                    c.TrangThai=g.Loai=="cap"?"da_ghi_chuoi":"thu_hoi";
                    if(g.Loai=="thu_hoi") { var r=await db.ThuHoiChungChis.SingleAsync(r=>r.GiaoDichId==g.Id,ct);r.NgayThuHoi=DateTime.UtcNow; }
                }
                catch(InvalidOperationException ex)
                {
                    g.TrangThai="failed";g.ThongBaoLoi=ex.Message[..Math.Min(500,ex.Message.Length)];
                    if(g.Loai=="cap")g.ChungChi.TrangThai="loi";
                    else { var r=await db.ThuHoiChungChis.SingleOrDefaultAsync(r=>r.GiaoDichId==g.Id,ct);if(r!=null)db.ThuHoiChungChis.Remove(r); }
                    logger.LogWarning(ex,"Giao dịch giả lập {Id} thất bại",g.Id);
                }
            }
            await db.SaveChangesAsync(ct);
            foreach(var id in pending.Select(g=>g.ChungChi.LoPhatHanhId).OfType<int>().Distinct())
            {
                var batch=await db.LoPhatHanhs.Include(l=>l.ChungChis).SingleAsync(l=>l.Id==id,ct);
                batch.TrangThai=batch.ChungChis.Any(c=>c.TrangThai=="loi")?"loi":batch.ChungChis.All(c=>c.TrangThai is "da_ghi_chuoi" or "thu_hoi")?"hoan_tat":"dang_xu_ly";
            }
            await db.SaveChangesAsync(ct);
        }
        finally { operationLock.Gate.Release(); }
    }
}
public sealed class GiaoDichGiaLapWorker(IServiceScopeFactory scopes, ILogger<GiaoDichGiaLapWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try { using var scope=scopes.CreateScope();await scope.ServiceProvider.GetRequiredService<PhatHanhGiaLapService>().XuLyAsync(stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogError(ex,"Chưa xử lý được giao dịch giả lập; sẽ thử lại."); }
            try { await Task.Delay(TimeSpan.FromSeconds(3),stoppingToken); } catch(OperationCanceledException) { break; }
        }
    }
}
