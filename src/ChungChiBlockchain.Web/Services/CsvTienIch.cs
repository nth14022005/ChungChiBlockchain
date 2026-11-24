using System.Text;

namespace ChungChiBlockchain.Web.Services;

/// <summary>Bộ đọc CSV nhỏ (không cần gói NuGet): hỗ trợ dấu ngoặc kép, ngoặc kép lồng nhau, xuống dòng trong ô.</summary>
public static class CsvTienIch
{
    /// <summary>Excel tiếng Việt thường lưu CSV bằng dấu chấm phẩy; tự đoán theo dòng tiêu đề.</summary>
    public static char PhatHienPhanCach(string text)
    {
        var dongDau = text.TrimStart('\uFEFF').Split('\n', 2)[0];
        return dongDau.Count(c => c == ';') > dongDau.Count(c => c == ',') ? ';' : ',';
    }

    /// <summary>Trả về các bản ghi không rỗng, kèm số dòng bắt đầu của bản ghi (dòng tiêu đề = 1).</summary>
    public static List<(int Dong, string[] Cot)> PhanTich(string text, char phanCach)
    {
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];

        var kq = new List<(int Dong, string[] Cot)>();
        var cot = new List<string>();
        var o = new StringBuilder();
        var trongNgoac = false;
        int dong = 1, dongBatDau = 1;

        void KetThucBanGhi()
        {
            cot.Add(o.ToString());
            o.Clear();
            if (cot.Any(c => !string.IsNullOrWhiteSpace(c)))
                kq.Add((dongBatDau, cot.ToArray()));
            cot.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (trongNgoac)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { o.Append('"'); i++; }
                    else trongNgoac = false;
                }
                else
                {
                    if (c == '\n') dong++;
                    o.Append(c);
                }
            }
            else if (c == '"' && o.Length == 0) trongNgoac = true;
            else if (c == phanCach) { cot.Add(o.ToString()); o.Clear(); }
            else if (c == '\r') { /* bỏ qua */ }
            else if (c == '\n') { KetThucBanGhi(); dong++; dongBatDau = dong; }
            else o.Append(c);
        }

        if (trongNgoac)
            throw new FormatException("File CSV có dấu ngoặc kép chưa được đóng.");
        if (o.Length > 0 || cot.Count > 0)
            KetThucBanGhi();
        return kq;
    }
}
