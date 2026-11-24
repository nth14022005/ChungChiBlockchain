using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services.Blockchain;
using ChungChiBlockchain.Web.Services.ChungChi;
using ChungChiBlockchain.Web.Services.Hash;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using ChungChiBlockchain.Web.Controllers;

public static class TestnetTests
{
    public static async Task Run(string root,string tmp,string fixturePath)
    {
        using var fixture=JsonDocument.Parse(File.ReadAllText(fixturePath));var f=fixture.RootElement;
        string F(string key)=>f.GetProperty(key).GetString()!;
        var config=new TestnetOptions{ChainId=1337,RpcUrl=F("rpcUrl"),ContractAddress=F("contractAddress"),Confirmations=2};
        var rpc=new EthereumRpcService(new HttpClient(),Options.Create(config));var owner=F("owner").ToLowerInvariant();
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);Console.WriteLine("PASS TESTNET: "+name);count++;}
        async Task Reject<T>(Func<Task> action,string name)where T:Exception{try{await action();}catch(T){Check(true,name);return;}throw new Exception("FAIL: "+name);}
        async Task<string> Send(string kind,string hash,string? contract=null)=> (await rpc.RpcAsync("eth_sendTransaction",[new{from=owner,to=contract??config.ContractAddress,data=EthereumRpcService.Data(kind,hash),gas="0x493e0",value="0x0"}])).GetString()!;
        async Task<ReceiptResult> Wait(string tx,string kind,string hash){for(int i=0;i<120;i++){try{var r=await rpc.ConfirmAsync(tx,config.ContractAddress,owner,kind,hash);if(r.Confirmed)return r;}catch(ChainUnavailableException){}await Task.Delay(300);}throw new Exception("Confirmation timeout");}
        await rpc.CheckContractAsync(config.ContractAddress);Check(true,"Contract version and chain validated");
        Check(EthereumRpcService.Same(await rpc.OwnerAsync(),owner),"Read contract owner");
        Check(await rpc.IsIssuerAsync(config.ContractAddress,owner),"Owner is issuer");
        Check(!await rpc.IsIssuerAsync(config.ContractAddress,F("otherWallet")),"Unapproved wallet is not issuer");
        var badNetwork=new EthereumRpcService(new HttpClient(),Options.Create(new TestnetOptions{ChainId=11155111,RpcUrl=config.RpcUrl,ContractAddress=config.ContractAddress}));
        await Reject<ChainUnavailableException>(()=>badNetwork.CheckNetworkAsync(),"Wrong RPC chain rejected");
        var dbOptions=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db=new AppDbContext(dbOptions);
        var org=new ToChuc{Id=1,MaToChuc="DNC",TenToChuc="Đại học",TrangThai="hoat_dong"};
        var student=new NguoiHoc{Id=1,ToChuc=org,MaNguoiHoc="SV01",HoTen="Đặng Thị Ánh"};
        var course=new KhoaHoc{Id=1,ToChuc=org,MaKhoaHoc="TEST",TenKhoaHoc="Testnet"};
        var template=new MauChungChi{Id=1,ToChuc=org,TenMau="Mẫu",TrangThai=true,LoaiChungChi=new LoaiChungChi{Id=1,TenLoai="Hoàn thành"}};
        var wallet=new ViBlockchain{Id=1,ToChuc=org,DiaChiVi=owner,TrangThai="hoat_dong"};db.AddRange(org,student,course,template,wallet);await db.SaveChangesAsync();
        var env=new TestEnvironment{ContentRootPath=root};var hasher=new HashServiceV1();
        var storage=new CertificateFileStorage(Options.Create(new FileStorageOptions{Root=Path.Combine(tmp,"testnet-files")}),env);
        var certs=new ChungChiService(db,new CertificatePdfService(env),storage,hasher,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"PublicBaseUrl","http://localhost:5087"}}).Build());
        var draft=await certs.TaoLoNhapAsync(1,1,new(){KhoaHocId=1,MauChungChiId=1,TenLo="Testnet",NguoiHocIds=new(){1}});var cert=draft.ChungChis.Single();
        var workflow=new TestnetWorkflow(db,rpc,rpc,certs,new DemoOperationLock());
        await Reject<InvalidOperationException>(()=>workflow.PrepareAsync(2,1,cert.Id,owner,"cap",null,default),"Other organization cannot prepare");
        await Reject<InvalidOperationException>(()=>workflow.PrepareAsync(1,1,cert.Id,F("otherWallet"),"cap",null,default),"Unregistered wallet cannot prepare");
        var request=await workflow.PrepareAsync(1,1,cert.Id,owner,"cap",null,default);Check(cert.TrangThai=="cho_xac_nhan","Intent saved before signing");
        var again=await workflow.PrepareAsync(1,1,cert.Id,owner,"cap",null,default);Check(request.RequestId==again.RequestId,"Wallet rejection can resume same intent");
        var tx=await Send("cap",cert.HashChungChi!);await Wait(tx,"cap",cert.HashChungChi!);
        // Simulate closing the browser before posting tx hash. The worker reconstructs it from indexed events.
        await workflow.ProcessAsync(default);Check(cert.TrangThai=="da_ghi_chuoi","Worker recovers issuance without browser callback");
        var saved=await db.GiaoDichBlockchains.SingleAsync();Check(EthereumRpcService.Same(saved.TxHash,tx),"Recovered exact transaction hash");
        await workflow.AttachAsync(1,request.RequestId,tx,default);Check(true,"Repeated callback is idempotent");
        await Reject<InvalidOperationException>(()=>workflow.AttachAsync(2,request.RequestId,tx,default),"Other organization cannot attach");
        Check((await rpc.TraCuuAsync(cert.HashChungChi!)).TrangThai==TrangThaiTrenChuoi.HopLe,"Real EVM verification valid");
        await Reject<TransactionMismatchException>(()=>rpc.ValidateTransactionAsync(tx,F("otherContract"),owner,"cap",cert.HashChungChi!),"Wrong contract rejected");
        await Reject<TransactionMismatchException>(()=>rpc.ValidateTransactionAsync(tx,config.ContractAddress,F("otherWallet"),"cap",cert.HashChungChi!),"Wrong sender rejected");
        await Reject<TransactionMismatchException>(()=>rpc.ValidateTransactionAsync(tx,config.ContractAddress,owner,"thu_hoi",cert.HashChungChi!),"Wrong operation rejected");
        await Reject<TransactionMismatchException>(()=>rpc.ValidateTransactionAsync(tx,config.ContractAddress,owner,"cap","0x"+new string('1',64)),"Wrong certificate hash rejected");
        var tampered=new EthereumRpcService(new HttpClient(new StripLogsHandler()),Options.Create(config));
        await Reject<TransactionMismatchException>(()=>tampered.ConfirmAsync(tx,config.ContractAddress,owner,"cap",cert.HashChungChi!),"Receipt without matching event rejected");
        var revert=await Send("cap",cert.HashChungChi!);Check((await Wait(revert,"cap",cert.HashChungChi!)).Failed,"Reverted transaction is failed, not successful");
        await Reject<InvalidOperationException>(()=>workflow.PrepareAsync(1,1,cert.Id,owner,"thu_hoi","",default),"Revoke requires reason");
        var fake=new GiaLapBlockchainService(Options.Create(new BlockchainOptions{DuongDanDuLieuGiaLap=Path.Combine(tmp,"mixed-chain.json")}),env,NullLogger<GiaLapBlockchainService>.Instance);
        var publicVerify=new VerifyController(db,fake,hasher,fake,rpc);
        var publicResult=(VerifyViewModel)((ViewResult)await publicVerify.Index(cert.MaChungChi,default)).Model!;
        Check(publicResult.KetQua=="hop_le"&&!publicResult.LaGiaLap,"Testnet certificate remains testnet when app mode is fake");
        var fakeFlow=new PhatHanhGiaLapService(db,fake,certs,new DemoOperationLock(),NullLogger<PhatHanhGiaLapService>.Instance);
        await fakeFlow.GuiAsync(1,1,cert.Id,null,"Wrong mode",default);
        Check(!await db.ThuHoiChungChis.AnyAsync(),"Fake mode cannot revoke testnet certificate");
        var revoke=await workflow.PrepareAsync(1,1,cert.Id,owner,"thu_hoi","Kiểm thử thu hồi",default);
        var tx2=await Send("thu_hoi",cert.HashChungChi!);await workflow.AttachAsync(1,revoke.RequestId,tx2,default);await Wait(tx2,"thu_hoi",cert.HashChungChi!);await workflow.ProcessAsync(default);
        Check(cert.TrangThai=="thu_hoi","Confirmed revocation updates certificate");
        Check((await rpc.TraCuuAsync(cert.HashChungChi!)).TrangThai==TrangThaiTrenChuoi.DaThuHoi,"Real EVM lookup revoked");
        Check((await rpc.TraCuuAsync("0x"+new string('e',64))).TrangThai==TrangThaiTrenChuoi.KhongTimThay,"Real EVM unknown hash");
        var revokedResult=(VerifyViewModel)((ViewResult)await publicVerify.Index(cert.MaChungChi,default)).Model!;
        Check(revokedResult.KetQua=="da_thu_hoi"&&!revokedResult.LaGiaLap,"Public verification uses original testnet after revocation");
        await HttpPageTests.Run(root,dbOptions,config,cert.Id);
        Console.WriteLine($"Testnet/EVM checks: {count}");
    }
    private sealed class StripLogsHandler:HttpMessageHandler
    {
        private readonly HttpClient inner=new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var body=await request.Content!.ReadAsStringAsync(ct);
            var response=await inner.PostAsync(request.RequestUri,new StringContent(body,Encoding.UTF8,"application/json"),ct);
            var data=JsonNode.Parse(await response.Content.ReadAsStringAsync(ct))!;
            if(body.Contains("eth_getTransactionReceipt")&&data["result"] is JsonObject obj)obj["logs"]=new JsonArray();
            return new HttpResponseMessage(response.StatusCode){Content=new StringContent(data.ToJsonString(),Encoding.UTF8,"application/json")};
        }
    }
}
