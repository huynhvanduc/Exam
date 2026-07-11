using Microsoft.Extensions.Configuration;

namespace Identity.API.Database;

internal static class DesignTimeConfiguration
{
    public static IConfiguration Build()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true);

        if (environmentName == "Development")
            builder.AddUserSecrets(typeof(Program).Assembly, optional: true);

        return builder.AddEnvironmentVariables().Build();
    }

    public static string GetIdentityConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString("IdentitySqlConnection")
            ?? throw new InvalidOperationException("Connection string 'IdentitySqlConnection' không được cấu hình.");
    }
}
