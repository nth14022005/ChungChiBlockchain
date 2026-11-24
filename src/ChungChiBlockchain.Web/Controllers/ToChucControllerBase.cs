using ChungChiBlockchain.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChungChiBlockchain.Web.Controllers;

/// <summary>
/// Nền cho các màn hình của tổ chức cấp. Tổ chức luôn lấy từ phiên đăng nhập, KHÔNG lấy từ form hay đường dẫn,
/// nên nhân viên không thể xem hoặc sửa dữ liệu của tổ chức khác dù có đoán được mã.
/// </summary>
public abstract class ToChucControllerBase : Controller
{
    protected int ToChucHienTai { get; private set; }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var tc = User.LayToChucId();
        if (tc is null)
        {
            context.Result = Forbid();
            return;
        }
        ToChucHienTai = tc.Value;
        base.OnActionExecuting(context);
    }
}
