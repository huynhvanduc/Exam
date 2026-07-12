using Identity.Server.Models;
using Identity.Server.Settings;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Server.Persistence
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

            ctx.SaveChanges();

            // Clients được đồng bộ lại toàn bộ mỗi lần khởi động (không chỉ thêm-nếu-thiếu),
            // vì cấu hình trong appsettings.json là nguồn sự thật duy nhất - tránh DB bị lệch
            // so với appsettings.json khi client đã tồn tại nhưng thuộc tính (redirect_uri,
            // require_consent...) đã đổi.
            var configuredClients = Config.GetClients(settings).ToList();
            var configuredClientIds = configuredClients.Select(c => c.ClientId).ToHashSet();

            var staleClients = ctx.Clients
                .Where(c => configuredClientIds.Contains(c.ClientId))
                .ToList();
            ctx.Clients.RemoveRange(staleClients);
            ctx.SaveChanges();

            foreach (var c in configuredClients)
                ctx.Clients.Add(c.ToEntity());

            ctx.SaveChanges();
        }
    }
}
