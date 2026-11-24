using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Services;
using System.Text.Json;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.Blockchain;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Controllers;
public sealed class VerifyViewModel
{
    public AiReview? Ai { get; set; }
    public string? Ma { get; set; }
    public string? KetQua { get; set; }
    public string? ThongBao { get; set; }
    public bool LaGiaLap { get; set; }
    public bool DaKiemTraFile { get; set; }
}
[AllowAnonymous, EnableRateLimiting("verify")]
public sealed class VerifyController(AppDbContext db, IBlockchainService chain, IHashService hash, GiaLapBlockchainService fake, EthereumRpcService rpc) : Controller
{
    [HttpGet("/verify/{ma?}")]
    public async Task<IActionResult> Index(string? ma, CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(ma)) return View(new VerifyViewModel{LaGiaLap=chain.LaGiaLap});
        return View(await CheckAsync(ma,null,"ma_qr",ct));
    }
    [HttpPost("/verify"), ValidateAntiForgeryToken, RequestSizeLimit(6*1024*1024), RequestFormLimits(MultipartBodyLengthLimit=6*1024*1024)]
    public async Task<IActionResult> KiemTra(string? ma, IFormFile? tep, CancellationToken ct, bool useAi = false)
    {
        byte[]? bytes=null;
        if(tep!=null)
        {
            if(tep.Length<5 || tep.Length>5*1024*1024 || !Path.GetExtension(tep.FileName).Equals(".pdf",StringComparison.OrdinalIgnoreCase))
                return View("Index",new VerifyViewModel{Ma=ma,LaGiaLap=chain.LaGiaLap,ThongBao="Chỉ nhận PDF tối đa 5 MB."});
            using var ms=new MemoryStream();await tep.CopyToAsync(ms,ct);bytes=ms.ToArray();
            if(!bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8)) return View("Index",new VerifyViewModel{Ma=ma,LaGiaLap=chain.LaGiaLap,ThongBao="Tệp không có định dạng PDF hợp lệ."});
        }
        var vm = await CheckAsync(ma, bytes, bytes is null ? "nhap_ma" : "tai_pdf", ct);
        if (useAi && bytes is not null)
        {
            var expected = new string?[5];
            var uploadedHash = hash.BamNoiDung(bytes);
            var code = ma?.Trim();
            // Nhân viên có thể đối chiếu PDF đã sửa với bản gốc trong tổ chức mình.
            var staffOrg = User.IsInRole(ChungChiBlockchain.Web.Models.VaiTroMa.ToChucCap)
                ? User.LayToChucId() : null;
            var cert = await db.ChungChis.AsNoTracking().FirstOrDefaultAsync(c =>
                (c.TrangThai == "da_ghi_chuoi" || c.TrangThai == "thu_hoi") &&
                ((c.HashChungChi == uploadedHash && (string.IsNullOrEmpty(code) || c.MaChungChi == code)) ||
                 (staffOrg.HasValue && c.NguoiHoc.ToChucId == staffOrg && c.MaChungChi == code)), ct);
            expected[0] = code;
            if (cert != null)
            {
                expected[0] = cert.MaChungChi;
                expected[3] = cert.NgayCap.ToString("yyyy-MM-dd");
                expected[4] = cert.XepLoai;
                try {
                    using var snapshot = JsonDocument.Parse(cert.DuLieuChungChi ?? "{}");
                    string? Field(string key) => snapshot.RootElement.ValueKind == JsonValueKind.Object &&
                        snapshot.RootElement.TryGetProperty(key, out var field) && field.ValueKind == JsonValueKind.String
                        ? field.GetString() : null;
                    expected[1] = Field("HoTen");
                    expected[2] = Field("TenKhoaHoc");
                } catch (JsonException) { /* Chứng chỉ cũ có thể thiếu bản chụp dữ liệu. */ }
            }
            vm.Ai = await HttpContext.RequestServices.GetRequiredService<CertificateReader>().ReadAsync(bytes, expected, vm.KetQua, ct);
        }
        return View("Index", vm);
    }
    private async Task<VerifyViewModel> CheckAsync(string? ma,byte[]? bytes,string method,CancellationToken ct)
    {
        ma=ma?.Trim();var vm=new VerifyViewModel{Ma=ma,LaGiaLap=chain.LaGiaLap,DaKiemTraFile=bytes!=null};
        if((string.IsNullOrEmpty(ma) && bytes is null) || ma?.Length>40) {vm.ThongBao="Nhập mã chứng chỉ tối đa 40 ký tự hoặc chọn PDF.";return vm;}
        var fileHash=bytes is null?null:hash.BamNoiDung(bytes);
        var cert=!string.IsNullOrEmpty(ma)?await db.ChungChis.AsNoTracking().SingleOrDefaultAsync(c=>c.MaChungChi==ma,ct):await db.ChungChis.AsNoTracking().SingleOrDefaultAsync(c=>c.HashChungChi==fileHash,ct);
        var lookup=fileHash??cert?.HashChungChi;
        string result="khong_tim_thay";
        var issued=cert is null?null:await db.GiaoDichBlockchains.AsNoTracking().Include(g=>g.HopDong).Include(g=>g.Vi)
            .Where(g=>g.ChungChiId==cert.Id && g.Loai=="cap" && g.TrangThai=="success").OrderByDescending(g=>g.Id).FirstOrDefaultAsync(ct);
        if(issued is not null)vm.LaGiaLap=issued.HopDong.ChainId==-1;
        if(cert is not null && cert.TrangThai is "da_ghi_chuoi" or "thu_hoi")
        {
            if(fileHash!=null && fileHash!=cert.HashChungChi) result="khong_khop";
            else
            {
                KetQuaTraCuuChuoi record;
                if(issued is null) {vm.ThongBao="Chưa có giao dịch cấp đã đối soát cho chứng chỉ này.";return vm;}
                try {
                    if(issued.HopDong.ChainId==-1) record=await fake.TraCuuAsync(cert.HashChungChi!,ct);
                    else {
                        if(issued.HopDong.ChainId!=rpc.Config.ChainId) {vm.ThongBao="Chứng chỉ thuộc mạng khác với RPC hiện cấu hình. Chưa thể xác minh.";return vm;}
                        // Re-check the original receipt as well as current state; avoid accepting an orphaned issuance.
                        if(issued.TxHash is null || issued.Vi is null) {vm.ThongBao="Thiếu chứng cứ giao dịch cấp. Chưa thể xác minh.";return vm;}
                        var evidence=await rpc.ConfirmAsync(issued.TxHash,issued.HopDong.DiaChiHopDong,issued.Vi.DiaChiVi,"cap",cert.HashChungChi!,ct);
                        if(!evidence.Confirmed||evidence.Failed) {vm.ThongBao="Giao dịch cấp chưa đủ xác nhận hoặc không còn hợp lệ. Chưa thể xác minh.";return vm;}
                        record=await rpc.LookupAsync(issued.HopDong.DiaChiHopDong,cert.HashChungChi!,ct);
                        if(!EthereumRpcService.Same(record.ViCap,issued.Vi.DiaChiVi)) {vm.ThongBao="Ví cấp trên chuỗi không khớp bản ghi phát hành.";return vm;}
                    }
                } catch(Exception ex) when(ex is ChainUnavailableException or TransactionMismatchException) {vm.ThongBao="Chưa thể xác minh trên testnet. "+ex.Message;return vm;}
                result=record.TrangThai switch {TrangThaiTrenChuoi.HopLe=>"hop_le",TrangThaiTrenChuoi.DaThuHoi=>"da_thu_hoi",_=>"khong_tim_thay"};
            }
        }
        vm.KetQua=result;
        vm.ThongBao=result switch {"hop_le"=>bytes is null?"Bản ghi hợp lệ. Chưa kiểm tra nội dung tệp PDF bạn đang giữ.":"PDF khớp với chứng chỉ đã phát hành và chưa bị thu hồi.","khong_khop"=>"PDF không khớp với mã chứng chỉ đã nhập.","da_thu_hoi"=>"Chứng chỉ đã bị thu hồi.",_=>"Không tìm thấy chứng chỉ đã phát hành phù hợp. Nếu PDF đã bị sửa, hãy nhập thêm mã gốc để đối chiếu."};
        // Không lưu mã/tệp người dùng nhập không tìm thấy; hash phải đúng định dạng của bảng.
        if(lookup!=null) {db.LichSuXacMinhs.Add(new LichSuXacMinh{ChungChiId=cert?.Id,HashTraCuu=lookup,KetQua=result,PhuongThuc=method,ThoiGian=DateTime.UtcNow});await db.SaveChangesAsync(ct);}
        return vm;
    }
}
