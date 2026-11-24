using ChungChiBlockchain.Web.Data;
using ChungChiBlockchain.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChungChiBlockchain.Web.Services;

public interface INhatKyService
{
    Task GhiAsync(int? nguoiDungId, string hanhDong, string? doiTuong = null, string? chiTiet = null);
}

/// <summary>Ghi vào bảng nhat_ky_hoat_dong. Lỗi ghi nhật ký không được làm hỏng chức năng chính.</summary>
public class NhatKyService : INhatKyService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<NhatKyService> _logger;

    public NhatKyService(AppDbContext db, IHttpContextAccessor accessor, ILogger<NhatKyService> logger)
    {
        _db = db;
        _accessor = accessor;
        _logger = logger;
    }

    public async Task GhiAsync(int? nguoiDungId, string hanhDong, string? doiTuong = null, string? chiTiet = null)
    {
        try
        {
            _db.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                NguoiDungId = nguoiDungId,
                HanhDong = Cat(hanhDong, 100)!,
                DoiTuong = Cat(doiTuong, 100),
                ChiTiet = Cat(chiTiet, 500),
                DiaChiIp = Cat(_accessor.HttpContext?.Connection.RemoteIpAddress?.ToString(), 45),
                ThoiGian = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _db.ChangeTracker.Clear();   // bỏ bản ghi lỗi để không ảnh hưởng các lần lưu sau trong cùng request
            _logger.LogWarning(ex, "Không ghi được nhật ký hoạt động: {HanhDong}", hanhDong);
        }
    }

    private static string? Cat(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
