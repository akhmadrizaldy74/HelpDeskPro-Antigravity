namespace HelpDeskPro.Web.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationState _anonymous;
    private readonly AuthenticationState _devState;
    private static readonly JwtSecurityTokenHandler _tokenHandler = new();
    private bool _isExplicitlyLoggedOut;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var devIdentity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "11111111-2222-3333-4444-555555555555"),
            new Claim(ClaimTypes.Email, "admin@helpdeskpro.local"),
            new Claim(ClaimTypes.Name, "Budi Santoso"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("departmentName", "IT Support")
        ], "devAuth", ClaimTypes.Name, ClaimTypes.Role);
        _devState = new AuthenticationState(new ClaimsPrincipal(devIdentity));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_isExplicitlyLoggedOut)
        {
            return _anonymous;
        }

        var token = await _localStorage.GetItemAsync("authToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            // Development fallback for interactive preview
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrEmpty(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                return _devState;
            }
            return _anonymous;
        }

        try
        {
            var jwt = _tokenHandler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync("authToken");
                return _anonymous;
            }

            var identity = new ClaimsIdentity(jwt.Claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            await _localStorage.RemoveItemAsync("authToken");
            return _anonymous;
        }
    }

    public async Task NotifyUserAuthentication(string token)
    {
        _isExplicitlyLoggedOut = false;
        await _localStorage.SetItemAsync("authToken", token);
        var authState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public async Task NotifyUserLogout()
    {
        _isExplicitlyLoggedOut = true;
        await _localStorage.RemoveItemAsync("authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }
}

