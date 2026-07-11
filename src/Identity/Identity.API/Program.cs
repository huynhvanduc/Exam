using Identity.API.Extensions;
using Identity.API.Persistence;
using Identity.API.Settings;
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
    // builder.Services.AddRazorPages();
    builder.Services.AddAuthorization();
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    builder.Services.Configure<IdentityServerSettings>(builder.Configuration.GetSection("IdentityServer"));
    builder.Services.ConfigureIdentity(builder.Configuration);
    builder.Services.ConfigureIdentityServer(builder.Configuration);

    var app = builder.Build();

    if (!EF.IsDesignTime)
        SeedData.EnsureSeedData(app);

    app.UseSerilogRequestLogging();

    app.UseRouting();

    app.UseIdentityServer();
    app.UseAuthorization();

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
