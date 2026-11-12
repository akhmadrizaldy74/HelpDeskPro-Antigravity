using HelpDeskPro.Web.Components;
using HelpDeskPro.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Autentikasi berbasis JWT yang disimpan di localStorage browser.
// Semua service di bawah ini scoped = satu instance per sirkuit Blazor.
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

var apiBaseUrl = new Uri((builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5017").TrimEnd('/') + "/");

// HttpMessageHandler dikelola oleh IHttpClientFactory (pooling koneksi), sedangkan
// AuthHeaderHandler dibuat di scope sirkuit agar dapat membaca token pengguna yang login.
// (Handler yang didaftarkan via AddHttpMessageHandler berjalan di scope terpisah sehingga
// tidak dapat mengakses localStorage / state sirkuit.)
builder.Services.AddHttpClient("HelpDeskProApi");
builder.Services.AddScoped<IApiClient>(sp =>
{
    var handlerFactory = sp.GetRequiredService<IHttpMessageHandlerFactory>();
    var authHandler = new AuthHeaderHandler(
        sp.GetRequiredService<TokenStore>(),
        sp.GetRequiredService<CustomAuthenticationStateProvider>())
    {
        InnerHandler = handlerFactory.CreateHandler("HelpDeskProApi")
    };

    var httpClient = new HttpClient(authHandler, disposeHandler: true)
    {
        BaseAddress = apiBaseUrl,
        Timeout = TimeSpan.FromSeconds(60)
    };

    return new ApiClient(httpClient);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

// Otorisasi halaman dilakukan di sisi komponen (AuthorizeRouteView) karena token
// hanya tersedia di browser; endpoint HTTP-nya sendiri tidak memerlukan autentikasi.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.Run();
