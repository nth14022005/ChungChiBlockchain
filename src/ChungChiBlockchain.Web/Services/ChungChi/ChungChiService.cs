using System.Globalization;
using System.Text;
using System.Text.Json;
using ChungChiBlockchain.Web.Data;
using ChungChiEntity = ChungChiBlockchain.Web.Models.Entities.ChungChi;
using LoPhatHanhEntity = ChungChiBlockchain.Web.Models.Entities.LoPhatHanh;
using TepChungChiEntity = ChungChiBlockchain.Web.Models.Entities.TepChungChi;
using ChungChiBlockchain.Web.Models.ViewModels;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services.ChungChi;

public interface ICertificatePdfService
{
    byte[] TaoPdf(ThongTinChungChiPdf thongTin, string urlXacMinh);
}

public record ThongTinChungChiPdf(
    string TenToChuc,
    string MaToChuc,
    string HoTenNguoiHoc,
    string MaNguoiHoc,
    string TenKhoaHoc,
    string? TenLoaiChungChi,
    string MaChungChi,
    DateOnly NgayCap,
    string? XepLoai);

public sealed class FileStorageOptions { public string Root { get; set; } = "../../storage/certificates"; }
public interface ICertificateFileStorage { Task<string> SaveAsync(string certificateCode, byte[] content, CancellationToken ct = default); string GetAbsolutePath(string relativePath); }
public sealed class CertificateFileStorage : ICertificateFileStorage
{
    private readonly string _root; public CertificateFileStorage(Microsoft.Extensions.Options.IOptions<FileStorageOptions> options, IWebHostEnvironment env) => _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Root));
    public async Task<string> SaveAsync(string code, byte[] content, CancellationToken ct = default) { Directory.CreateDirectory(_root); var name = code + ".pdf"; var path = Path.Combine(_root, name); if (File.Exists(path)) throw new IOException("Tệp chứng chỉ đã tồn tại."); bool created=false; try { await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None); created=true; await stream.WriteAsync(content, ct); } catch { if(created) File.Delete(path); throw; } return Path.Combine("storage", "certificates", name).Replace('\\', '/'); }
    public string GetAbsolutePath(string relativePath) => Path.GetFullPath(Path.Combine(_root, Path.GetFileName(relativePath)));
}

public record LoPhatHanhResult(bool ThanhCong, string? Loi, int? LoId, IReadOnlyList<ChungChiEntity> ChungChis);
public interface IChungChiService
{
    Task<List<ChungChiDanhSachItem>> LietKeAsync(int toChucId);
    Task<ChungChiTaoFormViewModel> TaoFormAsync(int toChucId);
    Task<LoPhatHanhResult> TaoLoNhapAsync(int toChucId, int nguoiTaoId, ChungChiTaoFormViewModel vm, CancellationToken ct = default);
    Task<byte[]?> LayFileAsync(int toChucId, int id, CancellationToken ct = default);
}
public sealed class ChungChiDanhSachItem { public int Id { get; set; } public string MaChungChi { get; set; } = ""; public string HoTen { get; set; } = ""; public string KhoaHoc { get; set; } = ""; public string TrangThai { get; set; } = ""; public DateOnly NgayCap { get; set; } public int? LoId { get; set; } public string? Hash { get; set; } }
public sealed class ChungChiTaoFormViewModel
{
    [System.ComponentModel.DataAnnotations.Required] public int KhoaHocId { get; set; }
    [System.ComponentModel.DataAnnotations.Required] public int MauChungChiId { get; set; }
    [System.ComponentModel.DataAnnotations.Required] public DateTime NgayCap { get; set; } = DateTime.Today;
    public string? XepLoai { get; set; }
    [System.ComponentModel.DataAnnotations.Required] public string TenLo { get; set; } = "";
    public List<int> NguoiHocIds { get; set; } = new();
    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> KhoaHocs { get; set; } = Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> MauChungChis { get; set; } = Enumerable.Empty<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    public List<NguoiHocChonItem> NguoiHocs { get; set; } = new();
}
public sealed class NguoiHocChonItem { public int Id { get; set; } public string Ma { get; set; } = ""; public string HoTen { get; set; } = ""; }

public sealed class ChungChiService : IChungChiService
{
    private readonly AppDbContext _db; private readonly ICertificatePdfService _pdf; private readonly ICertificateFileStorage _files; private readonly IHashService _hash; private readonly IConfiguration _config;
    public ChungChiService(AppDbContext db, ICertificatePdfService pdf, ICertificateFileStorage files, IHashService hash, IConfiguration config) { _db = db; _pdf = pdf; _files = files; _hash = hash; _config = config; }
    public Task<List<ChungChiDanhSachItem>> LietKeAsync(int tc) => _db.ChungChis.AsNoTracking().Where(x => x.NguoiHoc.ToChucId == tc).OrderByDescending(x => x.Id).Select(x => new ChungChiDanhSachItem { Id=x.Id, LoId=x.LoPhatHanhId, MaChungChi=x.MaChungChi, HoTen=x.NguoiHoc.HoTen, KhoaHoc=x.KhoaHoc.TenKhoaHoc, TrangThai=x.TrangThai, NgayCap=x.NgayCap, Hash=x.HashChungChi }).ToListAsync();
    public async Task<ChungChiTaoFormViewModel> TaoFormAsync(int tc) => new() { KhoaHocs = await _db.KhoaHocs.Where(x=>x.ToChucId==tc).OrderBy(x=>x.TenKhoaHoc).Select(x=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.TenKhoaHoc,x.Id.ToString())).ToListAsync(), MauChungChis = await _db.MauChungChis.Where(x=>x.ToChucId==tc && x.TrangThai).OrderBy(x=>x.TenMau).Select(x=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.TenMau,x.Id.ToString())).ToListAsync(), NguoiHocs = await _db.NguoiHocs.Where(x=>x.ToChucId==tc).OrderBy(x=>x.MaNguoiHoc).Select(x=>new NguoiHocChonItem {Id=x.Id,Ma=x.MaNguoiHoc,HoTen=x.HoTen}).ToListAsync() };
    public async Task<LoPhatHanhResult> TaoLoNhapAsync(int tc, int nguoiTao, ChungChiTaoFormViewModel vm, CancellationToken ct = default)
    {
        if (vm.NguoiHocIds.Count == 0 || vm.NguoiHocIds.Count > 100) return new(false,"Chọn từ 1 đến 100 người học.",null,Array.Empty<ChungChiEntity>());
        var org = await _db.ToChucs.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==tc,ct); var course=await _db.KhoaHocs.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==vm.KhoaHocId&&x.ToChucId==tc,ct); var template=await _db.MauChungChis.AsNoTracking().Include(x=>x.LoaiChungChi).SingleOrDefaultAsync(x=>x.Id==vm.MauChungChiId&&x.ToChucId==tc&&x.TrangThai,ct);
        if(org is null||course is null||template is null) return new(false,"Khóa học hoặc mẫu chứng chỉ không hợp lệ.",null,Array.Empty<ChungChiEntity>());
        var students=await _db.NguoiHocs.Where(x=>x.ToChucId==tc&&vm.NguoiHocIds.Contains(x.Id)).ToListAsync(ct); if(students.Count!=vm.NguoiHocIds.Distinct().Count()) return new(false,"Có người học không thuộc tổ chức.",null,Array.Empty<ChungChiEntity>());
        var used=await _db.ChungChis.Where(x=>vm.NguoiHocIds.Contains(x.NguoiHocId)&&x.KhoaHocId==vm.KhoaHocId&&x.TrangThai!="thu_hoi").Select(x=>x.NguoiHocId).ToListAsync(ct); if(used.Count>0) return new(false,"Một số người học đã có chứng chỉ của khóa học này chưa thu hồi.",null,Array.Empty<ChungChiEntity>());
        var lo=new LoPhatHanhEntity{ToChucId=tc,NguoiTaoId=nguoiTao,TenLo=vm.TenLo.Trim(),SoLuong=students.Count,TrangThai="nhap",NgayTao=DateTime.Now}; _db.LoPhatHanhs.Add(lo);
        var baseUrl = _config["PublicBaseUrl"]?.TrimEnd('/');
        if (VerificationLink.Create(baseUrl, "test") is null)
            return new(false,"Cần cấu hình PublicBaseUrl là địa chỉ web đầy đủ.",null,Array.Empty<ChungChiEntity>());
        var savedFiles = new List<string>();
        try {
        var created=new List<ChungChiEntity>(); var date=DateOnly.FromDateTime(vm.NgayCap); foreach(var s in students){ var code=await TaoMaAsync(org.MaToChuc,date.Year,ct); var snapshot=JsonSerializer.Serialize(new {s.MaNguoiHoc,s.HoTen,s.NgaySinh,s.Email,course.MaKhoaHoc,course.TenKhoaHoc,template.TenMau,NgayCap=date,vm.XepLoai}); var pdf=_pdf.TaoPdf(new(org.TenToChuc,org.MaToChuc,s.HoTen,s.MaNguoiHoc,course.TenKhoaHoc,template.LoaiChungChi.TenLoai,code,date,vm.XepLoai), VerificationLink.Create(baseUrl, code)!); var hash=_hash.BamNoiDung(pdf); var rel=await _files.SaveAsync(code,pdf,ct); savedFiles.Add(rel); var c=new ChungChiEntity {MaChungChi=code,NguoiHocId=s.Id,KhoaHocId=course.Id,MauChungChiId=template.Id,LoPhatHanh=lo,NguoiCapId=nguoiTao,NgayCap=date,XepLoai=vm.XepLoai,HashChungChi=hash,PhienBanBam=_hash.PhienBanHienTai,DuLieuChungChi=snapshot,TrangThai="nhap",NgayTao=DateTime.Now}; c.TepChungChis.Add(new TepChungChiEntity{TenTep=code+".pdf",DuongDan=rel,KichThuoc=pdf.LongLength,Sha256=hash[2..],NgayTao=DateTime.Now}); created.Add(c); _db.ChungChis.Add(c); }
        await _db.SaveChangesAsync(ct); return new(true,null,lo.Id,created);
        } catch(Exception ex) when(ex is DbUpdateException or IOException or InvalidOperationException or OperationCanceledException) {
            foreach(var file in savedFiles) { try { File.Delete(_files.GetAbsolutePath(file)); } catch(IOException) { /* Xem hướng dẫn dọn file mồ côi. */ } }
            _db.ChangeTracker.Clear();
            if(ex is OperationCanceledException) throw;
            return new(false, ex is InvalidOperationException ? ex.Message : "Không lưu được lô. Kiểm tra kết nối CSDL và quyền ghi thư mục storage.",null,Array.Empty<ChungChiEntity>());
        }
    }
    private async Task<string> TaoMaAsync(string org,int year,CancellationToken ct){ for(var i=0;i<20;i++){var r=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)); var m=$"{org.ToUpperInvariant()}-{year}-{r}"; if(!await _db.ChungChis.AnyAsync(x=>x.MaChungChi==m,ct))return m;} throw new InvalidOperationException("Không tạo được mã chứng chỉ duy nhất."); }
    public async Task<byte[]?> LayFileAsync(int tc,int id,CancellationToken ct=default){var f=await _db.TepChungChis.Include(x=>x.ChungChi).ThenInclude(x=>x.NguoiHoc).FirstOrDefaultAsync(x=>x.ChungChiId==id&&x.ChungChi.NguoiHoc.ToChucId==tc,ct); if(f is null)return null; var path=_files.GetAbsolutePath(f.DuongDan); if(!File.Exists(path))return null; var b=await File.ReadAllBytesAsync(path,ct); return _hash.BamNoiDung(b)==f.ChungChi.HashChungChi?b:null;}
}
