using Identity.Server.Components;
using Identity.Server.Extensions;
using Identity.Server.Persistence;
using Identity.Server.Settings;
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
    Log.Information("Starting Identity.Server");

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

    // YARP (Exam.WebApp) forward các request browser-facing dưới prefix "/idp" và strip prefix trước khi
    // forward tới đây, gắn header X-Proxy-Base-Path báo cho biết prefix đó. Không dùng tên chuẩn
    // "X-Forwarded-Prefix" vì đó là header YARP tự set mặc định theo PathBase THẬT của request gốc (luôn
    // rỗng ở đây vì "/idp" nằm trong Path chứ không phải PathBase phía Exam.WebApp) - dùng trùng tên sẽ bị
    // giá trị mặc định (rỗng) ghi đè lại. Không có middleware dựng sẵn nào đọc header tuỳ biến này, nên set
    // PathBase thủ công để mọi URL sinh ra bên trong (ReturnUrl, cookie path, <base href> ở App.razor) tự
    // cộng lại "/idp". Truy cập trực tiếp (không qua proxy) không có header này nên không bị ảnh hưởng.
    app.Use((context, next) =>
    {
        var prefix = context.Request.Headers["X-Proxy-Base-Path"].ToString();
        if (!string.IsNullOrEmpty(prefix))
            context.Request.PathBase = prefix;

        return next();
    });

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
    Log.Fatal(ex, "Identity.Server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
