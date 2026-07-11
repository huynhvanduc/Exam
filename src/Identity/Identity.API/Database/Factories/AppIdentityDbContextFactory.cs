using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.API.Database.Factories;

public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build();
        var connectionString = configuration.GetIdentityConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppIdentityDbContext(optionsBuilder.Options);
    }
}
