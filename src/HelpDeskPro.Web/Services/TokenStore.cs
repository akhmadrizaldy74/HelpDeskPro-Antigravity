namespace HelpDeskPro.Web.Services;

/// <summary>
/// Penyimpanan token JWT di memori untuk satu sirkuit Blazor (scoped).
/// Menghindari pemanggilan JS interop pada setiap request HTTP ke API.
/// </summary>
public class TokenStore
{
    public string? Token { get; private set; }

    public bool IsLoaded { get; private set; }

    public void Set(string? token)
    {
        Token = string.IsNullOrWhiteSpace(token) ? null : token;
        IsLoaded = true;
    }

    public void Clear() => Set(null);
}
