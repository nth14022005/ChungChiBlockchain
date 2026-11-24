using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.QuanTri)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IBlockchainService _chain;
    private readonly INhatKyService _nhatKy;

    public AdminController(AppDbContext db, IBlockchainService chain, INhatKyService nhatKy)
    {
        _db = db;
        _chain = chain;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new AdminDashboardViewModel
        {
            SoNguoiDung = await _db.NguoiDungs.CountAsync(),
            SoToChuc = await _db.ToChucs.CountAsync(),
            SoViBlockchain = await _db.ViBlockchains.CountAsync(),
            CheDoBlockchain = _chain.LaGiaLap ? "Giả lập" : "Mạng thật",
            TenMang = _chain.TenMang,
            BlockchainSanSang = await _chain.SanSangAsync(),
            KetQuaThu = TempData["KetQuaThu"] as string
        };
        return View(vm);
    }

    public async Task<IActionResult> ThongKe(CancellationToken ct)
    {
        var vm = new BaoCaoHoatDong
        {
            TrangThai = await _db.ChungChis.AsNoTracking().GroupBy(c => c.TrangThai)
                .Select(g => new DongDem(g.Key, g.Count())).ToListAsync(ct),
            XacMinh = await _db.LichSuXacMinhs.AsNoTracking().GroupBy(x => x.KetQua)
                .Select(g => new DongDem(g.Key, g.Count())).ToListAsync(ct),
            LichSu = await _db.LichSuXacMinhs.AsNoTracking().OrderByDescending(x => x.Id)
                .Take(50).Select(x => new DongLichSu(x.ThoiGian, x.ChungChi == null ? null : x.ChungChi.MaChungChi, x.PhuongThuc, x.KetQua)).ToListAsync(ct),
            NhatKy = await _db.NhatKyHoatDongs.AsNoTracking().OrderByDescending(x => x.Id)
                .Take(50).Select(x => new DongNhatKy(x.ThoiGian, x.NguoiDungId, x.HanhDong, x.DoiTuong)).ToListAsync(ct)
        };
        return View(vm);
    }

    /// <summary>
    /// Chạy thử chuỗi giả lập: cấp → (chờ xác nhận) → tra cứu → cấp trùng → thu hồi → (chờ) → tra cứu.
    /// Nếu cấu hình DoTreXacNhanGiay > 0, sẽ thấy rõ giai đoạn "đang chờ" rồi mới "thành công".
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuChuoiGiaLap()
    {
        if (_chain is not GiaLapBlockchainService gl)
        {
            TempData["KetQuaThu"] = "Chế độ blockchain hiện tại không hỗ trợ chạy thử giả lập.";
            return RedirectToAction(nameof(Index));
        }

        var ct = HttpContext.RequestAborted;
        var hash = "0x" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var sb = new StringBuilder();
        sb.AppendLine($"Hash thử: {hash}");

        try
        {
            var tx1 = await gl.GhiCapAsync(hash);
            sb.AppendLine($"1. Cấp chứng chỉ → giao dịch {Rut(tx1)}");
            sb.AppendLine("2. Ngay sau khi gửi → " + await MoTaAsync(gl, tx1, hash));
            await ChoXacNhanAsync(gl, tx1, ct);
            sb.AppendLine("3. Sau khi xác nhận → " + await MoTaAsync(gl, tx1, hash));

            try
            {
                await gl.GhiCapAsync(hash);
                sb.AppendLine("4. Cấp trùng hash → LỖI: lẽ ra phải bị từ chối");
            }
            catch (InvalidOperationException)
            {
                sb.AppendLine("4. Cấp trùng hash → bị từ chối (đúng như contract)");
            }

            var tx2 = await gl.GhiThuHoiAsync(hash);
            sb.AppendLine($"5. Thu hồi → giao dịch {Rut(tx2)}");
            await ChoXacNhanAsync(gl, tx2, ct);
            sb.AppendLine("6. Sau khi xác nhận → " + await MoTaAsync(gl, tx2, hash));
            sb.AppendLine("7. Hash chưa từng cấp → tra cứu: " +
                (await gl.TraCuuAsync("0x" + new string('a', 64), ct)).TrangThai);
        }
        catch (InvalidOperationException ex)
        {
            sb.AppendLine("LỖI: " + ex.Message);
        }

        int? id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var v) ? v : null;
        await _nhatKy.GhiAsync(id, "Chạy thử chuỗi giả lập", "blockchain");

        TempData["KetQuaThu"] = sb.ToString();
        return RedirectToAction(nameof(Index));
    }

    private static async Task<string> MoTaAsync(IBlockchainService chain, string tx, string hash)
    {
        var gd = await chain.LayTrangThaiGiaoDichAsync(tx);
        var tc = await chain.TraCuuAsync(hash);
        return $"giao dịch: {gd.TrangThai}, tra cứu: {tc.TrangThai}";
    }

    /// <summary>Chờ tối đa 15 giây cho giao dịch được xác nhận (giống việc chờ khối trên mạng thật).</summary>
    private static async Task ChoXacNhanAsync(IBlockchainService chain, string tx, CancellationToken ct)
    {
        var han = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < han)
        {
            var gd = await chain.LayTrangThaiGiaoDichAsync(tx, ct);
            if (gd.TrangThai != TrangThaiGiaoDich.DangCho) return;
            await Task.Delay(300, ct);
        }
    }

    private static string Rut(string tx) => tx.Length > 18 ? tx[..10] + "…" + tx[^6..] : tx;
}
