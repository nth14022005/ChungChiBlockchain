using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Blockchain;
using ChungChiBlockchain.Web.Services.Hash;
using ChungChiBlockchain.Web.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public static class WorkflowTests
{
    public static async Task Run(string root,string tmp)
    {
        void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var org=new ToChuc{Id=1,MaToChuc="DNC",TenToChuc="Đại học",TrangThai="hoat_dong"};
        var student=new NguoiHoc{Id=1,ToChuc=org,MaNguoiHoc="SV01",HoTen="Đặng Thị Ánh"};
        var course=new KhoaHoc{Id=1,ToChuc=org,MaKhoaHoc="A",TenKhoaHoc="Khóa A"};
        var course2=new KhoaHoc{Id=2,ToChuc=org,MaKhoaHoc="B",TenKhoaHoc="Khóa B"};
        var template=new MauChungChi{Id=1,ToChuc=org,TenMau="Mẫu",TrangThai=true,LoaiChungChi=new LoaiChungChi{Id=1,TenLoai="Hoàn thành"}};
        db.AddRange(org,student,course,course2,template);await db.SaveChangesAsync();
        var env=new TestEnvironment{ContentRootPath=root};var hash=new HashServiceV1();
        var files=new CertificateFileStorage(Options.Create(new FileStorageOptions{Root=Path.Combine(tmp,"workflow-files")}),env);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"PublicBaseUrl","http://localhost:5087"}}).Build();
        var certificates=new ChungChiService(db,new CertificatePdfService(env),files,hash,config);
        ChungChiTaoFormViewModel Form(int courseId)=>new(){KhoaHocId=courseId,MauChungChiId=1,TenLo="Lô thử",NguoiHocIds=new(){1},NgayCap=new DateTime(2026,11,2)};
        var draft=await certificates.TaoLoNhapAsync(1,1,Form(1));Check(draft.ThanhCong,"Create draft with PDF");var c=draft.ChungChis.Single();
        Check(!(await certificates.TaoLoNhapAsync(1,1,Form(1))).ThanhCong,"Same-course duplicate rejected");
        Check((await certificates.TaoLoNhapAsync(1,1,Form(2))).ThanhCong,"Different course allowed");
        Check(await certificates.LayFileAsync(2,c.Id) is null,"Cross-organization PDF denied");
        var chainOptions=Options.Create(new BlockchainOptions{DuongDanDuLieuGiaLap=Path.Combine(tmp,"workflow-chain.json"),DoTreXacNhanGiay=1});
        var chain=new GiaLapBlockchainService(chainOptions,env,NullLogger<GiaLapBlockchainService>.Instance);
        var flow=new PhatHanhGiaLapService(db,chain,certificates,new DemoOperationLock(),NullLogger<PhatHanhGiaLapService>.Instance);
        await flow.GuiAsync(2,1,c.Id,null,null,default);Check(!await db.GiaoDichBlockchains.AnyAsync(),"Cross-organization issue denied");
        await flow.GuiAsync(1,1,c.Id,null,null,default);Check(c.TrangThai=="cho_xac_nhan","Issue intent pending");
        await flow.GuiAsync(1,1,c.Id,null,null,default);Check(await db.GiaoDichBlockchains.CountAsync()==1,"Repeated issue click does not duplicate intent");
        var verify=new VerifyController(db,chain,hash,chain,new EthereumRpcService(new HttpClient(),Options.Create(new TestnetOptions())));
        var before=(VerifyViewModel)((ViewResult)await verify.Index(c.MaChungChi,default)).Model!;Check(before.KetQua=="khong_tim_thay","Pending is not valid");
        await flow.XuLyAsync(default);Check(c.TrangThai=="cho_xac_nhan","Delayed chain remains pending");await Task.Delay(1100);await flow.XuLyAsync(default);
        Check(c.TrangThai=="da_ghi_chuoi","Worker confirms issuance");
        var lookup=(VerifyViewModel)((ViewResult)await verify.Index(c.MaChungChi,default)).Model!;Check(lookup.KetQua=="hop_le"&&!lookup.DaKiemTraFile,"Code lookup distinguishes file verification");
        var bytes=(await certificates.LayFileAsync(1,c.Id))!;
        async Task<VerifyViewModel> Upload(byte[] data,string? code){using var stream=new MemoryStream(data);var file=new FormFile(stream,0,data.Length,"tep","certificate.pdf");return (VerifyViewModel)((ViewResult)await verify.KiemTra(code,file,default)).Model!;}
        Check((await Upload(bytes,null)).KetQua=="hop_le","PDF-only verification");
        bytes[^2]^=1;Check((await Upload(bytes,c.MaChungChi)).KetQua=="khong_khop","Modified PDF with code mismatch");
        Check((await Upload(bytes,null)).KetQua=="khong_tim_thay","Modified PDF without code unknown");
        await flow.GuiAsync(2,1,c.Id,null,"Sai quyền",default);Check(await db.ThuHoiChungChis.CountAsync()==0,"Cross-organization revocation denied");
        await flow.GuiAsync(1,1,c.Id,null,"",default);Check(await db.ThuHoiChungChis.CountAsync()==0,"Empty revoke reason denied");
        await flow.GuiAsync(1,1,c.Id,null,"Cấp nhầm thông tin",default);await flow.XuLyAsync(default);await Task.Delay(1100);await flow.XuLyAsync(default);
        Check(c.TrangThai=="thu_hoi","Worker confirms revocation");
        var revoked=(VerifyViewModel)((ViewResult)await verify.Index(c.MaChungChi,default)).Model!;Check(revoked.KetQua=="da_thu_hoi","Public lookup sees revoked");
        Check((await certificates.TaoLoNhapAsync(1,1,Form(1))).ThanhCong,"Can reissue after revocation");
    }
}
