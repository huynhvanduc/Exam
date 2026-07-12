using Identity.API.Components;
using Identity.API.Extensions;
using Identity.API.Persistence;
using Identity.API.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Identity.API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    builder.Services.AddRazorComponents();
    builder.Services.AddAuthorization();
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection("IdentityServer"));
    builder.Services.ConfigureIdentity(builder.Configuration);
    builder.Services.ConfigureIdentityServer(builder.Configuration);

    // PostConfigure luôn chạy sau mọi Configure khác (bất kể thứ tự đăng ký), đảm bảo SameSite=Lax
    // thắng dù AddAspNetIdentity()/AddIdentityServer() có tự đặt lại None ở đâu đó bên trong.
    builder.Services.PostConfigureAll<CookieAuthenticationOptions>(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    var app = builder.Build();

    if (!EF.IsDesignTime)
        SeedData.EnsureSeedData(app);

    app.UseSerilogRequestLogging();

    app.UseRouting();
    app.UseStaticFiles();

    app.UseIdentityServer();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapRazorComponents<App>();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Identity.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
