using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Hash;
using ChungChiBlockchain.Web.Services.Blockchain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Security.Cryptography;

var root=Path.GetFullPath(args.Length>0?args[0]:"src/ChungChiBlockchain.Web");
var tmp=Path.Combine(Path.GetTempPath(),"certificate-tests-"+Guid.NewGuid());Directory.CreateDirectory(tmp);
var env=new TestEnvironment{ContentRootPath=root};int count=0;
void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
async Task Reject(Func<Task> f,string name){try{await f();}catch(InvalidOperationException){Check(true,name);return;}throw new Exception("FAIL: "+name);}
var pdf=new CertificatePdfService(env);var hash=new HashServiceV1();
var info=new ThongTinChungChiPdf("Trường Đại học Nam Cần Thơ","DNC","Đặng Thị Hồng Ánh","SV001","Ứng dụng công nghệ Blockchain","Chứng chỉ hoàn thành","DNC-2026-1234ABCD",new DateOnly(2026,11,2),"Giỏi");
var bytes=pdf.TaoPdf(info,"http://localhost:5087/verify/DNC-2026-1234ABCD");
Check(bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8),"PDF header");
Check(bytes.SequenceEqual(pdf.TaoPdf(info,"http://localhost:5087/verify/DNC-2026-1234ABCD")),"PDF deterministic");
Check(bytes.SequenceEqual(pdf.TaoPdf(info with {HoTenNguoiHoc=info.HoTenNguoiHoc.Normalize(NormalizationForm.FormD)},"http://localhost:5087/verify/DNC-2026-1234ABCD")),"Vietnamese normalization");
var original=hash.BamNoiDung(bytes);var changed=(byte[])bytes.Clone();changed[^2]^=1;Check(hash.BamNoiDung(changed)!=original,"One-byte mutation changes hash");
File.WriteAllBytes(Path.Combine(tmp,"sample.pdf"),bytes);
File.WriteAllBytes(Path.Combine(tmp,"long.pdf"),pdf.TaoPdf(info with{TenKhoaHoc=new string('A',80)},"https://example.org/verify/DNC-2026-1234ABCD"));
var options=Options.Create(new BlockchainOptions{DuongDanDuLieuGiaLap=Path.Combine(tmp,"chain.json"),DoTreXacNhanGiay=0});
GiaLapBlockchainService NewChain()=>new(options,env,NullLogger<GiaLapBlockchainService>.Instance);
var chain=NewChain();var issuer="0x"+new string('1',40);var tx="0x"+new string('2',64);
await chain.GhiCapAsync(original,issuer,tx);
Check((await chain.TraCuuAsync(original)).TrangThai==TrangThaiTrenChuoi.HopLe,"Issue and verify");
Check(await chain.GhiCapAsync(original,issuer,tx)==tx,"Idempotent issue replay");
await Reject(()=>chain.GhiCapAsync(original,issuer),"Duplicate issue rejected");
await Reject(()=>chain.GhiThuHoiAsync(original,"0x"+new string('3',40)),"Wrong issuer cannot revoke");
var revoke="0x"+new string('4',64);await chain.GhiThuHoiAsync(original,issuer,revoke);
Check(await chain.GhiThuHoiAsync(original,issuer,revoke)==revoke,"Idempotent revoke replay");
Check((await NewChain().TraCuuAsync(original)).TrangThai==TrangThaiTrenChuoi.DaThuHoi,"Revocation survives restart");
Check((await chain.TraCuuAsync(hash.BamNoiDung(changed))).TrangThai==TrangThaiTrenChuoi.KhongTimThay,"Unknown hash");
var files=new CertificateFileStorage(Options.Create(new FileStorageOptions{Root=Path.Combine(tmp,"files")}),env);
await files.SaveAsync("TEST",bytes);bool overwrite=false;try{await files.SaveAsync("TEST",changed);}catch(IOException){overwrite=true;}Check(overwrite,"PDF cannot be overwritten");
await CertificateReaderTests.Run(bytes);
await WorkflowTests.Run(root,tmp);
if(args.Length>1)await TestnetTests.Run(root,tmp,args[1]);
Console.WriteLine($"Core tests: {count}; PDF samples: {tmp}");
public sealed class TestEnvironment : IWebHostEnvironment
{
    public string ApplicationName{get;set;}="Tests";public string EnvironmentName{get;set;}="Development";
    public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    public string WebRootPath{get;set;}="";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
}
