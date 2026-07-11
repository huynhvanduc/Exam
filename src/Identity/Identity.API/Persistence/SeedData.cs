using Identity.API.Database;
using Identity.API.Models;
using Identity.API.Settings;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.API.Persistence
{
    public static class SeedData
    {
        public static void EnsureSeedData(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;

            sp.GetRequiredService<AppIdentityDbContext>().Database.Migrate();
            sp.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            var config = sp.GetRequiredService<ConfigurationDbContext>();
            config.Database.Migrate();

            var idsSettings = sp.GetRequiredService<IOptions<IdentityServerSettings>>().Value;

            SeedConfiguration(config, idsSettings);
            SeedUsers(sp);
        }

        private static void SeedUsers(IServiceProvider sp)
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            if (userManager.FindByNameAsync("admin").Result is not null)
                return;

            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@.com.vn",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "1"
            };

            var result = userManager.CreateAsync(admin, "Admin@123$")
                .Result;

            if (!result.Succeeded)
                throw new Exception("Seed admin thất bại: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        private static void SeedConfiguration(ConfigurationDbContext ctx, IdentityServerSettings settings)
        {
            var existingIdentityResourceNames = ctx.IdentityResources.Select(r => r.Name).ToHashSet();
            foreach (var r in Config.GetIdentityResources())
            {
                if (existingIdentityResourceNames.Add(r.Name))
                    ctx.IdentityResources.Add(r.ToEntity());
            }

            var existingApiScopeNames = ctx.ApiScopes.Select(s => s.Name).ToHashSet();
            foreach (var s in Config.GetApiScopes(settings))
            {
                if (existingApiScopeNames.Add(s.Name))
                    ctx.ApiScopes.Add(s.ToEntity());
            }

            var existingApiResourceNames = ctx.ApiResources.Select(a => a.Name).ToHashSet();
            foreach (var a in Config.GetApiResources(settings))
            {
                if (existingApiResourceNames.Add(a.Name))
                    ctx.ApiResources.Add(a.ToEntity());
            }

            var existingClientIds = ctx.Clients.Select(c => c.ClientId).ToHashSet();
            foreach (var c in Config.GetClients(settings))
            {
                if (existingClientIds.Add(c.ClientId))
                    ctx.Clients.Add(c.ToEntity());
            }

            ctx.SaveChanges();
        }
    }
}
