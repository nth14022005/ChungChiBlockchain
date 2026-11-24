namespace ChungChiBlockchain.Web.Services;

public static class VerificationLink
{
    public static string? Create(string? baseUrl, string code)
    {
        if (!Uri.TryCreate(baseUrl?.TrimEnd('/'), UriKind.Absolute, out var uri) ||
            !(uri.Scheme == "http" || uri.Scheme == "https") ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
            return null;
        return uri.AbsoluteUri.TrimEnd('/') + "/verify/" + Uri.EscapeDataString(code);
    }
}
