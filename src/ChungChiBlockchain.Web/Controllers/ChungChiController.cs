using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Data;
using Microsoft.EntityFrameworkCore;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.ChungChi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.ToChucCap)]
public sealed class ChungChiController : ToChucControllerBase
{
    private readonly IChungChiService _service;
    private readonly INhatKyService _nhatKy;
    public ChungChiController(IChungChiService service, INhatKyService nhatKy) { _service = service; _nhatKy = nhatKy; }

    public async Task<IActionResult> Index() => View(await _service.LietKeAsync(ToChucHienTai));

    [HttpGet]
    public async Task<IActionResult> MaQr(int id, [FromServices] AppDbContext db,
        [FromServices] IConfiguration config, CancellationToken ct)
    {
        var code = await db.ChungChis.AsNoTracking()
            .Where(c => c.Id == id && c.NguoiHoc.ToChucId == ToChucHienTai &&
                (c.TrangThai == "da_ghi_chuoi" || c.TrangThai == "thu_hoi"))
            .Select(c => c.MaChungChi).SingleOrDefaultAsync(ct);
        if (code is null) return NotFound();
        var url = VerificationLink.Create(config["PublicBaseUrl"], code);
        if (url is null) return BadRequest("Kiểm tra PublicBaseUrl.");
        using var generator = new QRCoder.QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qr = new QRCoder.PngByteQRCode(data);
        return File(qr.GetGraphic(8), "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> TaoLo() => View(await _service.TaoFormAsync(ToChucHienTai));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoLo(ChungChiTaoFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var form = await _service.TaoFormAsync(ToChucHienTai); form.KhoaHocId=vm.KhoaHocId; form.MauChungChiId=vm.MauChungChiId; form.NgayCap=vm.NgayCap; form.XepLoai=vm.XepLoai; form.TenLo=vm.TenLo; form.NguoiHocIds=vm.NguoiHocIds; return View(form);
        }
        var id = User.LayId(); if (id is null) return Forbid();
        var kq = await _service.TaoLoNhapAsync(ToChucHienTai, id.Value, vm, ct);
        if (!kq.ThanhCong) { ModelState.AddModelError("", kq.Loi!); var form=await _service.TaoFormAsync(ToChucHienTai); form.KhoaHocId=vm.KhoaHocId;form.MauChungChiId=vm.MauChungChiId;form.NgayCap=vm.NgayCap;form.XepLoai=vm.XepLoai;form.TenLo=vm.TenLo;form.NguoiHocIds=vm.NguoiHocIds;return View(form); }
        await _nhatKy.GhiAsync(id, "Tạo lô chứng chỉ nháp", "lo_phat_hanh", kq.LoId?.ToString());
        TempData["ThongBao"] = $"Đã tạo lô nháp gồm {kq.ChungChis.Count} chứng chỉ."; return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> TaiPdf(int id, CancellationToken ct)
    {
        var bytes=await _service.LayFileAsync(ToChucHienTai,id,ct); return bytes is null ? NotFound("Tệp không tồn tại hoặc đã bị thay đổi.") : File(bytes,"application/pdf",$"chung-chi-{id}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhatHanh(int? id, int? loId, [FromServices] PhatHanhGiaLapService flow, CancellationToken ct)
    {
        var user=User.LayId();if(user is null)return Forbid();
        TempData["ThongBao"]=await flow.GuiAsync(ToChucHienTai,user.Value,id,loId,null,ct);
        await _nhatKy.GhiAsync(user,"Yêu cầu phát hành giả lập","chung_chi",id?.ToString()??loId?.ToString());
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuHoi(int id, string? lyDo, [FromServices] PhatHanhGiaLapService flow, CancellationToken ct)
    {
        var user=User.LayId();if(user is null)return Forbid();
        TempData["ThongBao"]=await flow.GuiAsync(ToChucHienTai,user.Value,id,null,lyDo??"",ct);
        await _nhatKy.GhiAsync(user,"Yêu cầu thu hồi giả lập","chung_chi",id.ToString());
        return RedirectToAction(nameof(Index));
    }
}
