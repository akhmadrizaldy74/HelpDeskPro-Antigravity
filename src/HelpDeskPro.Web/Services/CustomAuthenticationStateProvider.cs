namespace HelpDeskPro.Web.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public const string TokenStorageKey = "authToken";

    private static readonly JwtSecurityTokenHandler _tokenHandler = new();
    private static readonly AuthenticationState _anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ILocalStorageService _localStorage;
    private readonly TokenStore _tokenStore;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage, TokenStore tokenStore)
    {
        _localStorage = localStorage;
        _tokenStore = tokenStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_tokenStore.IsLoaded)
        {
            var stored = await _localStorage.GetItemAsync(TokenStorageKey);
            _tokenStore.Set(stored);
        }

        var token = _tokenStore.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return _anonymous;
        }

        var principal = BuildPrincipal(token);
        if (principal is null)
        {
            _tokenStore.Clear();
            await _localStorage.RemoveItemAsync(TokenStorageKey);
            return _anonymous;
        }

        return new AuthenticationState(principal);
    }

    public async Task NotifyUserAuthentication(string token)
    {
        _tokenStore.Set(token);
        await _localStorage.SetItemAsync(TokenStorageKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyUserLogout()
    {
        _tokenStore.Clear();
        await _localStorage.RemoveItemAsync(TokenStorageKey);
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    private static ClaimsPrincipal? BuildPrincipal(string token)
    {
        try
        {
            var jwt = _tokenHandler.ReadJwtToken(token);
            if (jwt.ValidTo <= DateTime.UtcNow)
            {
                return null;
            }

            // Token ditulis dengan nama klaim pendek (unique_name, role, nameid, email);
            // petakan kembali ke ClaimTypes standar agar Identity.Name & IsInRole berfungsi.
            var claims = jwt.Claims.Select(c =>
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(c.Type, out var mapped)
                    ? new Claim(mapped, c.Value, c.ValueType, c.Issuer)
                    : c);

            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
