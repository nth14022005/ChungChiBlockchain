using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ChungChiBlockchain.Web.Services.Blockchain;

public sealed class TestnetOptions
{
    public string RpcUrl { get; set; } = "";
    public long ChainId { get; set; } = 11155111;
    public string ContractAddress { get; set; } = "";
    public int Confirmations { get; set; } = 2;
}
public sealed class ChainUnavailableException(string message) : Exception(message);
public sealed class TransactionMismatchException(string message) : Exception(message);
public sealed record ReceiptResult(bool Confirmed, bool Failed, long? BlockNumber);

/// <summary>Chỉ đọc JSON-RPC. Không có khóa riêng và không gửi giao dịch.</summary>
public sealed class EthereumRpcService(HttpClient http, IOptions<TestnetOptions> options) : IBlockchainService
{
    public TestnetOptions Config => options.Value;
    public bool LaGiaLap => false;
    public string TenMang => Config.ChainId == 11155111 ? "Sepolia testnet" : "Local EVM (kiểm thử)";
    public const string IssuedTopic = "0x4eddb87fd718c9c020afc202869de93e0aa57722e2389a82288eb0b8cb35d0c9";
    public const string RevokedTopic = "0x86e31385c8dde141636e5662d2638d6e541f0db140a8fcbce62a6d62c7b103bb";
    public static bool AddressValid(string? s) => s is not null && Regex.IsMatch(s, "^0x[0-9a-fA-F]{40}$") && s[2..].Any(c=>c!='0');
    public static string Address(string s) => AddressValid(s) ? s.ToLowerInvariant() : throw new InvalidOperationException("Địa chỉ ví/hợp đồng không hợp lệ.");
    public static string Data(string kind, string hash) => (kind == "cap" ? "0x0f75e81f" : kind == "thu_hoi" ? "0xb75c7dc6" : throw new InvalidOperationException("Loại giao dịch không hợp lệ.")) + DinhDangHash.ChuanHoaHoacLoi(hash)[2..];
    public static string TopicAddress(string wallet) => "0x" + Address(wallet)[2..].PadLeft(64,'0');
    public static long Number(string s) => long.Parse(s.StartsWith("0x") ? s[2..] : s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    public static string Hex(long n) => "0x" + n.ToString("x",CultureInfo.InvariantCulture);
    public static bool Same(string? a,string? b) => string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
    public void ValidateConfiguration()
    {
        if (!Uri.TryCreate(Config.RpcUrl,UriKind.Absolute,out var uri) || (uri.Scheme!="https" && !(uri.Scheme=="http" && uri.IsLoopback)))
            throw new ChainUnavailableException("Chưa cấu hình Testnet:RpcUrl HTTPS hợp lệ trong VS Code.");
        if (Config.ChainId!=11155111 && !(uri.IsLoopback && Config.ChainId==1337))
            throw new ChainUnavailableException("Bản này chỉ hỗ trợ Sepolia; mạng 1337 chỉ dành cho kiểm thử localhost. Không dùng mainnet.");
        if(!AddressValid(Config.ContractAddress))throw new ChainUnavailableException("Chưa cấu hình địa chỉ CertificateRegistry đã triển khai.");
    }
    public async Task<JsonElement> RpcAsync(string method,object[] parameters,CancellationToken ct=default)
    {
        ValidateConfiguration();
        try
        {
            var payload = new { jsonrpc = "2.0", id = 1, method, @params = parameters };
using var first = await http.PostAsJsonAsync(Config.RpcUrl, payload, ct);

HttpResponseMessage? retry = null;
if ((int)first.StatusCode == 429)
{
    var retryAfter = first.Headers.RetryAfter;
    var delay = retryAfter?.Delta
        ?? (retryAfter?.Date is DateTimeOffset date
            ? date - DateTimeOffset.UtcNow
            : TimeSpan.FromSeconds(3));

    if (delay > TimeSpan.FromSeconds(10))
        throw new ChainUnavailableException(
            $"RPC đang giới hạn lượt đọc. Hãy thử lại sau {Math.Ceiling(delay.TotalSeconds)} giây.");

    await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, ct);
    retry = await http.PostAsJsonAsync(Config.RpcUrl, payload, ct);
}

using var retryResponse = retry;
var response = retryResponse ?? first;
            if (!response.IsSuccessStatusCode)
    throw new ChainUnavailableException(
        $"RPC {method} trả HTTP {(int)response.StatusCode}.");
            using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if(doc.RootElement.TryGetProperty("error",out _))throw new ChainUnavailableException("RPC từ chối yêu cầu. Kiểm tra cấu hình, hợp đồng và hạn mức truy cập.");
            if(!doc.RootElement.TryGetProperty("result",out var result))throw new ChainUnavailableException("RPC trả dữ liệu không hợp lệ.");
            return result.Clone();
        }
        catch(HttpRequestException){throw new ChainUnavailableException("Không kết nối được RPC. Chưa thể xác minh trạng thái blockchain.");}
        catch(TaskCanceledException) when(!ct.IsCancellationRequested){throw new ChainUnavailableException("RPC quá thời gian chờ. Giao dịch chưa bị kết luận thất bại.");}
        catch(JsonException){throw new ChainUnavailableException("RPC trả dữ liệu không hợp lệ.");}
    }
    public async Task CheckNetworkAsync(CancellationToken ct=default)
    {
        if(Number((await RpcAsync("eth_chainId",[],ct)).GetString()!)!=Config.ChainId)throw new ChainUnavailableException("RPC đang trỏ sai mạng so với ChainId.");
    }
    public async Task<string> CallAsync(string contract,string data,CancellationToken ct=default) =>
        (await RpcAsync("eth_call",[new {to=Address(contract),data},"latest"],ct)).GetString()!;
    public async Task CheckContractAsync(string contract,CancellationToken ct=default)
    {
        await CheckNetworkAsync(ct);
        var code=(await RpcAsync("eth_getCode",[Address(contract),"latest"],ct)).GetString();
        if(code is null or "0x" or "0x0")throw new ChainUnavailableException("Địa chỉ cấu hình chưa có smart contract trên mạng này.");
        var version=await CallAsync(contract,"0x54fd4d50",ct);
        if(version.Length<194 || !version[130..].StartsWith("312e302e30",StringComparison.OrdinalIgnoreCase))
            throw new ChainUnavailableException("Hợp đồng không trả phiên bản CertificateRegistry 1.0.0.");
    }
    public async Task<bool> SanSangAsync(CancellationToken ct=default) {try{await CheckContractAsync(Config.ContractAddress,ct);return true;}catch(ChainUnavailableException){return false;}}
    public async Task<long> HeadAsync(CancellationToken ct=default)=>Number((await RpcAsync("eth_blockNumber",[],ct)).GetString()!);
    public async Task<bool> IsIssuerAsync(string contract,string wallet,CancellationToken ct=default) => Number(await CallAsync(contract,"0x877b9a67"+Address(wallet)[2..].PadLeft(64,'0'),ct))==1;
    public async Task<string> OwnerAsync(CancellationToken ct=default) {var data=await CallAsync(Config.ContractAddress,"0x8da5cb5b",ct);return Address("0x"+data[^40..]);}
    public Task<KetQuaTraCuuChuoi> TraCuuAsync(string hash,CancellationToken ct=default)=>LookupAsync(Config.ContractAddress,hash,ct);
    public async Task<KetQuaTraCuuChuoi> LookupAsync(string contract,string hash,CancellationToken ct=default)
    {
        await CheckNetworkAsync(ct);
        var result=await CallAsync(contract,"0x75e36616"+DinhDangHash.ChuanHoaHoacLoi(hash)[2..],ct);
        if(result.Length!=258)throw new ChainUnavailableException("Dữ liệu verify từ hợp đồng không hợp lệ.");
        var status=Number(result.Substring(2,64));
        if(status<0||status>2)throw new ChainUnavailableException("Trạng thái contract không hợp lệ.");
        var issued=Number(result.Substring(130,64));var revoked=Number(result.Substring(194,64));
        return new((TrangThaiTrenChuoi)status,status==0?null:"0x"+result.Substring(90,40),issued==0?null:DateTimeOffset.FromUnixTimeSeconds(issued),revoked==0?null:DateTimeOffset.FromUnixTimeSeconds(revoked));
    }
    public async Task ValidateTransactionAsync(string tx,string contract,string wallet,string kind,string hash,CancellationToken ct=default)
    {
        await CheckNetworkAsync(ct);
        var t=await RpcAsync("eth_getTransactionByHash",[DinhDangHash.ChuanHoaHoacLoi(tx)],ct);
        if(t.ValueKind==JsonValueKind.Null)throw new ChainUnavailableException("RPC chưa thấy giao dịch. Giữ mã giao dịch và thử đồng bộ lại; không ký thêm giao dịch mới.");
        if(!Same(t.GetProperty("hash").GetString(),tx)||!Same(t.GetProperty("to").GetString(),contract)||!Same(t.GetProperty("from").GetString(),wallet)||!Same(t.GetProperty("input").GetString(),Data(kind,hash))||Number(t.GetProperty("value").GetString()!)!=0)
            throw new TransactionMismatchException("Giao dịch không khớp hợp đồng, ví cấp, thao tác hoặc hash chứng chỉ.");
        if(t.TryGetProperty("chainId",out var cid)&&Number(cid.GetString()!)!=Config.ChainId)throw new TransactionMismatchException("Giao dịch thuộc mạng khác.");
    }
    public async Task<ReceiptResult> ConfirmAsync(string tx,string contract,string wallet,string kind,string hash,CancellationToken ct=default)
    {
        await ValidateTransactionAsync(tx,contract,wallet,kind,hash,ct);
        var r=await RpcAsync("eth_getTransactionReceipt",[tx],ct);
        if(r.ValueKind==JsonValueKind.Null)return new(false,false,null);
        if(!Same(r.GetProperty("transactionHash").GetString(),tx)||!Same(r.GetProperty("to").GetString(),contract)||!Same(r.GetProperty("from").GetString(),wallet))throw new TransactionMismatchException("Receipt không khớp giao dịch.");
        var block=Number(r.GetProperty("blockNumber").GetString()!);
        if(await HeadAsync(ct)-block+1<Math.Max(2,Config.Confirmations))return new(false,false,block);
        var canonical=await RpcAsync("eth_getBlockByNumber",[Hex(block),false],ct);
        if(canonical.ValueKind==JsonValueKind.Null || !Same(canonical.GetProperty("hash").GetString(),r.GetProperty("blockHash").GetString()))return new(false,false,block);
        if(Number(r.GetProperty("status").GetString()!)==0)return new(true,true,block);
        var expected=kind=="cap"?IssuedTopic:RevokedTopic;
        var found=r.GetProperty("logs").EnumerateArray().Any(l=>Same(l.GetProperty("address").GetString(),contract)&&l.GetProperty("topics").GetArrayLength()==3&&Same(l.GetProperty("topics")[0].GetString(),expected)&&Same(l.GetProperty("topics")[1].GetString(),hash)&&Same(l.GetProperty("topics")[2].GetString(),TopicAddress(wallet)));
        if(!found)throw new TransactionMismatchException("Receipt thành công nhưng thiếu sự kiện chứng chỉ mong đợi.");
        return new(true,false,block);
    }
    public async Task<(string? Tx,long Next)> FindEventAsync(string contract,string wallet,string kind,string hash,long from,CancellationToken ct)
    {
        await CheckNetworkAsync(ct);
        var end=Math.Min(await HeadAsync(ct)-Math.Max(2,Config.Confirmations)+1,from+1999);
        if(end<from)return (null,from);
        var logs=await RpcAsync("eth_getLogs",[new{address=Address(contract),fromBlock=Hex(from),toBlock=Hex(end),topics=new[]{kind=="cap"?IssuedTopic:RevokedTopic,hash,TopicAddress(wallet)}}],ct);
        foreach(var log in logs.EnumerateArray())
        {
            if(log.TryGetProperty("removed",out var removed)&&removed.GetBoolean())continue;
            return (log.GetProperty("transactionHash").GetString(),from);
        }
        return (null,end+1);
    }
    public async Task<KetQuaGiaoDich> LayTrangThaiGiaoDichAsync(string tx,CancellationToken ct=default)
    {
        await CheckNetworkAsync(ct);var r=await RpcAsync("eth_getTransactionReceipt",[DinhDangHash.ChuanHoaHoacLoi(tx)],ct);
        if(r.ValueKind==JsonValueKind.Null)return new(TrangThaiGiaoDich.DangCho,null,null);
        var block=Number(r.GetProperty("blockNumber").GetString()!);
        if(await HeadAsync(ct)-block+1<Math.Max(2,Config.Confirmations))return new(TrangThaiGiaoDich.DangCho,block,null);
        return new(Number(r.GetProperty("status").GetString()!)==1?TrangThaiGiaoDich.ThanhCong:TrangThaiGiaoDich.ThatBai,block,null);
    }
}
