namespace HelpDeskPro.Web.Services;

using System.Net;
using System.Net.Http.Headers;

/// <summary>
/// Menambahkan header Authorization: Bearer ke setiap request API dan
/// mengakhiri sesi bila API menolak token (401).
/// Dibuat per sirkuit (lihat Program.cs) agar dapat mengakses state sirkuit Blazor.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly TokenStore _tokenStore;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthHeaderHandler(TokenStore tokenStore, CustomAuthenticationStateProvider authStateProvider)
    {
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_tokenStore.IsLoaded)
        {
            // Memastikan token sudah dibaca dari localStorage sebelum request pertama.
            await _authStateProvider.GetAuthenticationStateAsync();
        }

        var token = _tokenStore.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        var isLoginRequest = request.RequestUri?.AbsolutePath.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) == true;
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isLoginRequest && !string.IsNullOrWhiteSpace(token))
        {
            // Token kedaluwarsa / tidak valid: logout -> AuthorizeRouteView mengarahkan ke /login.
            await _authStateProvider.NotifyUserLogout();
        }

        return response;
    }
}
