using System.Text;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChungChiBlockchain.Web.Controllers;

[Authorize(Roles = VaiTroMa.ToChucCap)]
public class NguoiHocController : ToChucControllerBase
{
    private const int KichThuocTrang = 20;
    private const long GioiHanFile = 1_000_000;   // 1 MB

    private readonly INguoiHocService _svc;
    private readonly INhatKyService _nhatKy;

    public NguoiHocController(INguoiHocService svc, INhatKyService nhatKy)
    {
        _svc = svc;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index(string? q, int trang = 1)
    {
        var (items, tong) = await _svc.LietKeAsync(ToChucHienTai, q, trang, KichThuocTrang);
        return View(new NguoiHocDanhSachViewModel
        {
            Items = items, TuKhoa = q, Trang = Math.Max(1, trang), TongSo = tong, KichThuoc = KichThuocTrang
        });
    }

    // ------------------------------------------------------------ thêm / sửa
    [HttpGet]
    public IActionResult Create() => View(new NguoiHocFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NguoiHocFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.TaoAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        // Nhật ký chỉ ghi mã, KHÔNG ghi họ tên/email/số điện thoại (dữ liệu cá nhân)
        await _nhatKy.GhiAsync(User.LayId(), "Thêm người học", "nguoi_hoc", vm.MaNguoiHoc.Trim().ToUpperInvariant());
        TempData["ThongBao"] = "Đã thêm người học.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _svc.LayFormAsync(ToChucHienTai, id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NguoiHocFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var kq = await _svc.CapNhatAsync(ToChucHienTai, vm);
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, kq.Loi!);
            return View(vm);
        }

        await _nhatKy.GhiAsync(User.LayId(), "Sửa người học", "nguoi_hoc", vm.Id.ToString());
        TempData["ThongBao"] = "Đã cập nhật người học.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------ nhập CSV
    [HttpGet]
    public IActionResult NhapCsv() => View(new NhapCsvViewModel());

    [HttpGet]
    public IActionResult MauCsv()
    {
        const string noiDung =
            "ma_nguoi_hoc,ho_ten,ngay_sinh,email,so_dien_thoai\r\n" +
            "SV001,Nguyễn Văn A,15/08/2004,vana@example.com,0901234567\r\n" +
            "SV002,Trần Thị B,2003-12-01,,\r\n";
        // Có BOM để Excel hiển thị đúng tiếng Việt
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(noiDung)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", "nguoi_hoc_mau.csv");
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> NhapCsv(IFormFile? file)
    {
        var vm = new NhapCsvViewModel { DaChay = true };

        if (file is null || file.Length == 0)
        {
            vm.ThongBaoChung = "Vui lòng chọn một file CSV.";
            return View(vm);
        }
        if (file.Length > GioiHanFile)
        {
            vm.ThongBaoChung = "File quá lớn (tối đa 1 MB).";
            return View(vm);
        }
        var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (duoi != ".csv" && duoi != ".txt")
        {
            vm.ThongBaoChung = "Chỉ chấp nhận file .csv.";
            return View(vm);
        }

        await using var stream = file.OpenReadStream();
        var kq = await _svc.NhapCsvAsync(ToChucHienTai, stream);

        vm.ThanhCong = kq.ThanhCong;
        vm.SoDongNhap = kq.SoDongNhap;
        vm.TongSoLoi = kq.TongSoLoi;
        vm.ThongBaoChung = kq.ThongBaoChung;
        vm.Loi = kq.Loi;

        if (kq.ThanhCong)
            await _nhatKy.GhiAsync(User.LayId(), "Nhập người học từ CSV", "nguoi_hoc", $"{kq.SoDongNhap} dòng");

        return View(vm);
    }
}
