using System.ComponentModel.DataAnnotations;
using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models;
using ChungChiBlockchain.Web.Models.Entities;
using ChungChiBlockchain.Web.Services;
using ChungChiBlockchain.Web.Services.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Controllers;
public sealed class WalletCertificatePage
{
    public Models.Entities.ChungChi Certificate {get;set;}=null!;
    public List<GiaoDichBlockchain> Transactions {get;set;}=[];
    public bool Testnet {get;set;}
}
public sealed class PrepareWalletForm
{
    public int CertificateId{get;set;}
    [Required,StringLength(42)]public string Wallet{get;set;}="";
    [Required,StringLength(10)]public string Kind{get;set;}="";
    [StringLength(500)]public string? Reason{get;set;}
}
public sealed class AttachWalletForm{public int RequestId{get;set;} [Required,StringLength(66)]public string TxHash{get;set;}="";}
[Authorize]
public sealed class BlockchainController(AppDbContext db,EthereumRpcService rpc,IBlockchainService chain,TestnetWorkflow workflow,INhatKyService audit):Controller
{
    private int ToChucHienTai => User.LayToChucId() ?? -1;
    [Authorize(Roles=VaiTroMa.ToChucCap),HttpGet]
    public async Task<IActionResult> ChungChi(int id,CancellationToken ct)
    {
        var c=await db.ChungChis.AsNoTracking().Include(x=>x.NguoiHoc).SingleOrDefaultAsync(x=>x.Id==id&&x.NguoiHoc.ToChucId==ToChucHienTai,ct);
        if(c is null)return NotFound();
        return View(new WalletCertificatePage{Certificate=c,Testnet=!chain.LaGiaLap,Transactions=await db.GiaoDichBlockchains.AsNoTracking().Include(g=>g.HopDong).Include(g=>g.Vi).Where(g=>g.ChungChiId==id).OrderByDescending(g=>g.Id).ToListAsync(ct)});
    }
    [Authorize(Roles=VaiTroMa.QuanTri),HttpGet]
    public async Task<IActionResult> Vi(CancellationToken ct)=>View(await db.ViBlockchains.AsNoTracking().Include(v=>v.ToChuc).OrderBy(v=>v.Id).ToListAsync(ct));
    [Authorize(Roles=VaiTroMa.QuanTri+","+VaiTroMa.ToChucCap),HttpGet]
    public async Task<IActionResult> Config(CancellationToken ct)
    {
        if(chain.LaGiaLap)return BadRequest(new{error="Đang ở chế độ giả lập. Xem HUONG_DAN_VSCODE.md để bật Testnet."});
        return await Safe(async()=>{await rpc.CheckContractAsync(rpc.Config.ContractAddress,ct);return new{chainId=rpc.Config.ChainId,contract=EthereumRpcService.Address(rpc.Config.ContractAddress),owner=await rpc.OwnerAsync(ct),confirmations=Math.Max(2,rpc.Config.Confirmations)};});
    }
    [Authorize(Roles=VaiTroMa.ToChucCap),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Prepare([FromBody]PrepareWalletForm form,CancellationToken ct)
    {
        if(!ModelState.IsValid)return BadRequest(new{error="Thông tin yêu cầu không hợp lệ."});
        return await Safe(async()=>{var r=await workflow.PrepareAsync(ToChucHienTai,User.LayId()??0,form.CertificateId,form.Wallet,form.Kind,form.Reason,ct);await audit.GhiAsync(User.LayId(),"Chuẩn bị giao dịch testnet","chung_chi",form.CertificateId.ToString());return r;});
    }
    [Authorize(Roles=VaiTroMa.ToChucCap),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Attach([FromBody]AttachWalletForm form,CancellationToken ct)
    {
        if(!ModelState.IsValid)return BadRequest(new{error="Mã giao dịch không hợp lệ."});
        return await Safe(async()=>{await workflow.AttachAsync(ToChucHienTai,form.RequestId,form.TxHash,ct);await audit.GhiAsync(User.LayId(),"Tiếp nhận giao dịch testnet","giao_dich_blockchain",form.RequestId.ToString());return new{message="Đã nhận giao dịch. Máy chủ sẽ đối chiếu và chờ đủ xác nhận."};});
    }
    [Authorize(Roles=VaiTroMa.QuanTri),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncIssuer(int id,CancellationToken ct)
    {
        if(chain.LaGiaLap)return BadRequest(new{error="Chỉ đồng bộ khi chạy chế độ Testnet."});
        return await Safe(async()=>{var wallet=await db.ViBlockchains.SingleOrDefaultAsync(v=>v.Id==id,ct)??throw new InvalidOperationException("Không tìm thấy ví.");await rpc.CheckContractAsync(rpc.Config.ContractAddress,ct);bool allowed=await rpc.IsIssuerAsync(rpc.Config.ContractAddress,wallet.DiaChiVi,ct);wallet.TrangThai=allowed?"hoat_dong":"bi_khoa";await db.SaveChangesAsync(ct);await audit.GhiAsync(User.LayId(),"Đồng bộ quyền ví từ testnet","vi_blockchain",id.ToString());return new{message=allowed?"Ví có quyền cấp trên contract; đã đồng bộ Hoạt động.":"Ví không có quyền cấp trên contract; đã đồng bộ Bị khóa."};});
    }
    private async Task<IActionResult> Safe(Func<Task<object>> action)
    {
        try{return Json(await action());}
        catch(Exception ex)when(ex is ChainUnavailableException or TransactionMismatchException or InvalidOperationException or ArgumentException){return BadRequest(new{error=ex.Message});}
        catch(DbUpdateException){return Conflict(new{error="Dữ liệu vừa thay đổi hoặc CSDL chưa cập nhật. Tải lại trang; kiểm tra script 003."});}
    }
}
