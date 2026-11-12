using System.Text;
using HelpDeskPro.Api.Middlewares;
using HelpDeskPro.Api.Services;
using HelpDeskPro.Application;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Infrastructure;
using HelpDeskPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "ThisIsASecretKeyForHelpDeskProSecuritySuperSecret2026!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HelpDeskPro",
        ValidAudience = jwtSettings["Audience"] ?? "HelpDeskProClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAgentOrAbove", policy =>
        policy.RequireRole("Agent", "Supervisor", "Admin"));

    options.AddPolicy("RequireSupervisorOrAbove", policy =>
        policy.RequireRole("Supervisor", "Admin"));

    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HelpDeskPro API",
        Version = "v1",
        Description = "API Dokumentasi HelpDeskPro"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login via POST /api/Auth/login: token terpasang otomatis. Jika mengisi manual, tempel token saja tanpa kata 'Bearer'."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Contoh body login terisi otomatis (akun seed Development) agar cukup klik "Execute".
    options.SchemaFilter<LoginExampleSchemaFilter>();
});

var app = builder.Build();

// Request logging dipasang sebelum exception middleware agar status code yang
// tercatat adalah status akhir (mis. 400/404/422), bukan 500.
app.UseSerilogRequestLogging();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HelpDeskPro API v1");
        c.DocumentTitle = "HelpDeskPro API";

        // Token tetap tersimpan saat halaman Swagger di-refresh.
        c.EnablePersistAuthorization();

        // Setelah login berhasil, token dari respons langsung dipasang ke tombol "Authorize",
        // jadi endpoint lain bisa langsung dicoba tanpa salin-tempel token.
        // Catatan: fungsi harus satu baris karena Swashbuckle menyisipkannya ke dalam string JSON.
        c.UseResponseInterceptor(
            "(res) => { try { if (res && res.ok && res.url && res.url.toLowerCase().indexOf('/api/auth/login') !== -1) { " +
            "var body = res.obj || (typeof res.data === 'string' ? JSON.parse(res.data) : res.data); " +
            "var token = body && body.data && body.data.accessToken; " +
            "var schema = token && window.ui && window.ui.specSelectors.securityDefinitions() && window.ui.specSelectors.securityDefinitions().get('Bearer'); " +
            "if (schema) { var a = window.ui.authActions; var payload = { Bearer: { name: 'Bearer', schema: schema, value: token } }; " +
            "(a.authorizeWithPersistOption || a.authorize)(payload); console.log('HelpDeskPro: token login dipasang otomatis.'); } } " +
            "} catch (e) { console.warn('HelpDeskPro: gagal memasang token otomatis', e); } return res; }");
    });

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (context.Database.IsRelational())
    {
        // SQL Server di Docker butuh beberapa detik sampai siap menerima koneksi
        // (mis. tepat setelah "docker compose up"), jadi migration dicoba ulang.
        const int maxAttempts = 20;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await context.Database.MigrateAsync();
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts &&
                ex is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
            {
                app.Logger.LogWarning("Database belum siap (percobaan {Attempt}/{Max}): {Message}. Mencoba lagi dalam 3 detik...",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Di Development API dilayani via HTTP (dipanggil oleh HelpDeskPro.Web di http://localhost:5017).
// Redirect ke HTTPS akan membuat HttpClient membuang header Authorization -> 401.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program;

/// <summary>
/// Mengisi contoh request login di Swagger dengan akun Admin hasil seeding (Development).
/// </summary>
internal sealed class LoginExampleSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(OpenApiSchema schema, Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext context)
    {
        if (context.Type != typeof(HelpDeskPro.Application.Features.Auth.Commands.Login.LoginCommand))
        {
            return;
        }

        schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
        {
            ["email"] = new Microsoft.OpenApi.Any.OpenApiString("admin@helpdeskpro.local"),
            ["password"] = new Microsoft.OpenApi.Any.OpenApiString("Dev@Passw0rd123!")
        };
    }
}
