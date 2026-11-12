namespace HelpDeskPro.Infrastructure;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Infrastructure.Persistence;
using HelpDeskPro.Infrastructure.Persistence.Interceptors;
using HelpDeskPro.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DatabaseSeeder>();

        services.Configure<Options.JwtOptions>(configuration.GetSection(Options.JwtOptions.SectionName));
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBusinessCalendar, BusinessCalendar>();
        services.AddScoped<ISlaCalculator, BusinessHoursSlaCalculator>();
        services.AddHostedService<SlaMonitorService>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseSqlServer(connectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
