using Identity.Server.Persistence;
using Identity.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Server.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentitySqlConnection");

        services.AddDbContext<AppIdentityDbContext>(opt =>
            opt.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
        {
            opt.Password.RequiredLength = 8;
            opt.Password.RequireDigit = true;
            opt.Password.RequireUppercase = false;
            opt.User.RequireUniqueEmail = true;

            opt.Lockout.MaxFailedAccessAttempts = 5;
            opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppIdentityDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
    }

    public static void ConfigureIdentityServer(this IServiceCollection services,
    IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentitySqlConnection");

        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

        services.AddIdentityServer(options => {
            options.IssuerUri = configuration["IdentityServer:IssuerUri"]!;
            options.Authentication.CookieLifetime = TimeSpan.FromHours(2);
            options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
        })
        .AddDeveloperSigningCredential()
        .AddAspNetIdentity<ApplicationUser>()
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                sql =>
                {
                    sql.MigrationsAssembly(migrationsAssembly);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
              sql =>
              {
                  sql.MigrationsAssembly(migrationsAssembly);
                  sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
              });
            options.EnableTokenCleanup = true;
            options.TokenCleanupInterval = 3600;
        })
        .AddConfigurationStoreCache();
    }
}
