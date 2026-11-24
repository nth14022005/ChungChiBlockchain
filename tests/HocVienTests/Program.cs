using System.Security.Claims;
using ChungChiBlockchain.Web.Controllers;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
await using var db = new AppDbContext(options);
var hash = new HashServiceV1();
var pdf = "%PDF-TEST"u8.ToArray();
var folder = Directory.CreateTempSubdirectory("hoc-vien-");
try
{
    var storage = new TestStorage(folder.FullName);
    await File.WriteAllBytesAsync(storage.GetAbsolutePath("a.pdf"), pdf);
    var role = new VaiTro { Id = 4, MaVaiTro = VaiTroHocVien.Ma, TenVaiTro = "Người học" };
    db.VaiTros.Add(role);
    db.KhoaHocs.Add(new KhoaHoc { Id = 1, ToChucId = 1, MaKhoaHoc = "K1", TenKhoaHoc = "Khóa học" });
    foreach (int i in new[]{1,2})
    {
        db.NguoiHocs.Add(new NguoiHoc { Id = i, ToChucId = 1, HoTen = "HV"+i, MaNguoiHoc = "HV"+i });
        db.TaiKhoanNguoiHocs.Add(new TaiKhoanNguoiHoc {
            NguoiHocId = i, NguoiDung = new NguoiDung { Id = 10+i, ToChucId = 1, VaiTro = role,
                TenDangNhap = "hv"+i, HoTen = "HV"+i, MatKhauHash="test", TrangThai = true }
        });
    }
    foreach (var (id, student, state) in new[]{ (1,1,"da_ghi_chuoi"), (2,2,"da_ghi_chuoi"), (3,1,"nhap"), (4,1,"thu_hoi") })
    {
        var c = new ChungChi { Id = id, MaChungChi = "TEST-"+id, KhoaHocId = 1, NguoiHocId = student,
            TrangThai = state, DuLieuChungChi = "{}", HashChungChi = hash.BamNoiDung(pdf) };
        c.TepChungChis.Add(new TepChungChi { TenTep = "a.pdf", DuongDan = "a.pdf", Sha256 = c.HashChungChi! });
        db.ChungChis.Add(c);
    }
    await db.SaveChangesAsync();
    var ctrl = new HocVienController(db,storage,hash);
    ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
        User = new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,"11")}, "test"))
    }};
    var list = (IReadOnlyList<ChungChiCaNhan>)((ViewResult)await ctrl.Index(default)).Model!;
    Check(list.Select(x=>x.Id).Order().SequenceEqual(new[]{1,4}), "Chỉ hiển thị chứng chỉ đã cấp của chính người học");
    Check(await ctrl.TaiPdf(2,default) is NotFoundResult,"Không tải PDF của người khác trong cùng tổ chức");
    Check(await ctrl.TaiPdf(3,default) is NotFoundResult,"Không tải PDF nháp");
    Check(await ctrl.TaiPdf(1,default) is FileContentResult own && own.FileContents.SequenceEqual(pdf),"Tải đúng PDF gốc");
    await File.WriteAllTextAsync(storage.GetAbsolutePath("a.pdf"),"modified");
    Check(await ctrl.TaiPdf(1,default) is NotFoundObjectResult,"Từ chối PDF lưu trữ sai mã băm");
}
finally { folder.Delete(true); }

static void Check(bool condition,string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}
sealed class TestStorage(string root) : ICertificateFileStorage
{
    public string GetAbsolutePath(string relativePath) => Path.Combine(root,Path.GetFileName(relativePath));
    public Task<string> SaveAsync(string code,byte[] bytes,CancellationToken ct=default) => throw new NotSupportedException();
}
