using System.Net;
using System.Text.Json;
using ChungChiBlockchain.Web.Services;
using Microsoft.Extensions.Configuration;

public static class CertificateReaderTests
{
    public static async Task Run(byte[] pdf)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string,string?> { ["CertificateAI:ApiKey"] = "test-key" }).Build();
        using var http = new HttpClient(new ModelStub(pdf));
        var reader = new CertificateReader(http, config);
        var result = await reader.ReadAsync(pdf, ["DNC-1","An","K1","2026-11-08","Giỏi"], "da_thu_hoi", default);
        if (result.Fields.Count != 5 || result.Fields[0].Comparison != "Khớp" ||
            result.Fields[1].Comparison != "Khác bản ghi" || result.Fields[4].Comparison != "Không đọc được" ||
            !result.Message.Contains("thu hồi") || string.IsNullOrEmpty(result.Explanation))
            throw new Exception("FAIL: AI field comparison or revoked result");
        Console.WriteLine("PASS: AI extraction, mismatch, empty field and independent revoked status (mock provider)");
    }

    private sealed class ModelStub(byte[] pdf) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            using var envelope = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var data = envelope.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("inline_data");
            if (!data.GetProperty("data").GetBytesFromBase64().SequenceEqual(pdf) ||
                !request.Headers.Contains("x-goog-api-key"))
                throw new Exception("FAIL: AI request does not contain submitted PDF or key header");
            var text = JsonSerializer.Serialize(new {code="DNC-1",name="Bình",course="K1",date="2026-11-08",grade="",explanation="Chứng chỉ đã bị thu hồi."});
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new {
                candidates = new[] { new { content = new { parts = new[] { new { text } } } } }
            })) };
        }
    }
}
