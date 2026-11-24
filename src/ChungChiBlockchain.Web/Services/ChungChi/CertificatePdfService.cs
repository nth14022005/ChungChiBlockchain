using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ChungChiBlockchain.Web.Services.ChungChi;

/// <summary>PDF với font TrueType nhúng, Unicode, độ rộng chữ và QR vector.</summary>
public sealed class CertificatePdfService : ICertificatePdfService
{
    private readonly byte[] _font;
    private readonly Dictionary<int, int[]> _map;
    public CertificatePdfService(IWebHostEnvironment env)
    {
        _font = File.ReadAllBytes(Path.Combine(env.ContentRootPath, "Assets", "DejaVuSerif.ttf"));
        _map = JsonSerializer.Deserialize<Dictionary<int, int[]>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Assets", "font-map.json")))!;
    }
    public byte[] TaoPdf(ThongTinChungChiPdf t, string urlXacMinh)
    {
        var b = new StringBuilder("q\n0.10 0.18 0.29 RG 2 w 35 35 525 772 re S\n0.10 0.18 0.29 rg\n");
        var used = new SortedSet<int>();
        void Text(string value, double y, double size, double maxWidth = 465)
        {
            value = value.Normalize(NormalizationForm.FormC);
            if (value.Any(c => !_map.ContainsKey(c))) throw new InvalidOperationException("Nội dung có ký tự font chưa hỗ trợ. Hãy kiểm tra tên và thông tin chứng chỉ.");
            double width = value.Sum(c => _map[c][1]) / 1000.0;
            size = Math.Min(size, maxWidth / Math.Max(width, 1));
            if(size < 7) throw new InvalidOperationException("Nội dung quá dài để in rõ trên chứng chỉ. Hãy rút gọn tên tổ chức hoặc khóa học.");
            foreach (var c in value) used.Add(c);
            var hex = string.Concat(value.Select(c => ((int)c).ToString("X4")));
            b.AppendLine(FormattableString.Invariant($"BT /F1 {size:0.###} Tf {(595-width*size)/2:0.###} {y} Td <{hex}> Tj ET"));
        }
        Text(t.TenToChuc, 752, 18);
        Text("CHỨNG CHỈ ĐIỆN TỬ", 678, 28);
        Text("CERTIFICATE OF COMPLETION", 647, 12);
        Text("Chứng nhận", 588, 14);
        Text(t.HoTenNguoiHoc, 548, 25);
        Text("Đã hoàn thành khóa học", 503, 14);
        Text(t.TenKhoaHoc, 465, 21);
        Text("Loại chứng chỉ: " + t.TenLoaiChungChi, 420, 12);
        Text("Xếp loại: " + (t.XepLoai ?? "—"), 390, 12);
        Text("Mã người học: " + t.MaNguoiHoc, 354, 11);
        Text("Ngày cấp: " + t.NgayCap.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 328, 11);
        Text("Mã chứng chỉ: " + t.MaChungChi, 302, 11);
        using var gen = new QRCoder.QRCodeGenerator();
        using var qr = gen.CreateQrCode(urlXacMinh, QRCoder.QRCodeGenerator.ECCLevel.Q);
        double scale = 136.0 / qr.ModuleMatrix.Count;
        b.AppendLine("0 0 0 rg");
        for(int y=0;y<qr.ModuleMatrix.Count;y++) for(int x=0;x<qr.ModuleMatrix.Count;x++)
            if(qr.ModuleMatrix[y][x]) b.AppendLine(FormattableString.Invariant($"{229+x*scale:0.###} {120+(qr.ModuleMatrix.Count-y-1)*scale:0.###} {scale:0.###} {scale:0.###} re f"));
        Text("Quét QR để tra cứu bản ghi chứng chỉ.", 96, 10);
        Text("Tải PDF lên trang xác minh để kiểm tra toàn vẹn tệp.", 78, 9);
        b.AppendLine("Q");
        var cidMap = new byte[131072];
        foreach(var c in used) { cidMap[2*c]=(byte)(_map[c][0]>>8);cidMap[2*c+1]=(byte)_map[c][0]; }
        var unicode = new StringBuilder("/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /Vietnamese def /CMapType 2 def\n1 begincodespacerange <0000> <FFFF> endcodespacerange\n");
        foreach(var chunk in used.Chunk(100)) { unicode.AppendLine($"{chunk.Length} beginbfchar");foreach(var c in chunk)unicode.AppendLine($"<{c:X4}> <{c:X4}>");unicode.AppendLine("endbfchar"); }
        unicode.AppendLine("endcmap CMapName currentdict /CMap defineresource pop end end");
        var widths=string.Join(" ",used.Select(c=>$"{c} [{_map[c][1]}]"));
        byte[] A(string s)=>Encoding.ASCII.GetBytes(s);
        byte[] Stream(byte[] data,string extra="") => A($"<< /Length {data.Length} {extra} >>\nstream\n").Concat(data).Concat(A("\nendstream")).ToArray();
        var objs=new List<byte[]> {
            A("<< /Type /Catalog /Pages 2 0 R >>"),
            A("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            A("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>"),
            Stream(A(b.ToString())),
            A("<< /Type /Font /Subtype /Type0 /BaseFont /DejaVuSerif /Encoding /Identity-H /DescendantFonts [6 0 R] /ToUnicode 10 0 R >>"),
            A($"<< /Type /Font /Subtype /CIDFontType2 /BaseFont /DejaVuSerif /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> /FontDescriptor 7 0 R /W [{widths}] /CIDToGIDMap 9 0 R >>"),
            A("<< /Type /FontDescriptor /FontName /DejaVuSerif /Flags 34 /FontBBox [-770 -347 2105 1109] /ItalicAngle 0 /Ascent 928 /Descent -236 /CapHeight 729 /StemV 80 /FontFile2 8 0 R >>"),
            Stream(_font,$"/Length1 {_font.Length}"),Stream(cidMap),Stream(A(unicode.ToString())) };
        using var ms=new MemoryStream();ms.Write(A("%PDF-1.4\n"));var offsets=new List<long>();
        for(int i=0;i<objs.Count;i++){offsets.Add(ms.Position);ms.Write(A($"{i+1} 0 obj\n"));ms.Write(objs[i]);ms.Write(A("\nendobj\n"));}
        var start=ms.Position;ms.Write(A($"xref\n0 {objs.Count+1}\n0000000000 65535 f \n"));
        foreach(var n in offsets)ms.Write(A(n.ToString("D10",CultureInfo.InvariantCulture)+" 00000 n \n"));
        ms.Write(A($"trailer << /Size {objs.Count+1} /Root 1 0 R >>\nstartxref\n{start}\n%%EOF\n"));return ms.ToArray();
    }
}
