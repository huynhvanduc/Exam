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

            // Id cố định để khớp với ExternalId của các user tương ứng đã được DataSeeder tạo sẵn bên
            // Exam service (role Admin/Student) - tránh việc lần đăng nhập đầu tiên bị coi là "first
            // user" và bị gán nhầm role, đồng thời tên hiển thị khớp ngay từ đầu.
            EnsureUser(userManager, "528ac9b1-20ff-4d42-bd6d-e85300acde89", "admin", "admin@.com.vn",
                "Admin", "1", "Admin@123$");
            EnsureUser(userManager, "33333333-3333-3333-3333-333333333333", "student1", "lan.nguyen@exam-platform.vn",
                "Lan", "Nguyễn", "Student@123");
            EnsureUser(userManager, "44444444-4444-4444-4444-444444444444", "student2", "hung.pham@exam-platform.vn",
                "Hùng", "Phạm", "Student@123");
            EnsureUser(userManager, "22222222-2222-2222-2222-222222222222", "instructor1", "minh.tran@exam-platform.vn",
                "Minh", "Trần", "Instructor@123");
            EnsureUser(userManager, "66666666-6666-6666-6666-666666666666", "instructor2", "tuan.le@exam-platform.vn",
                "Tuấn", "Lê", "Instructor@123");
        }

        private static void EnsureUser(UserManager<ApplicationUser> userManager, string id, string userName,
            string email, string firstName, string lastName, string password)
        {
            if (userManager.FindByNameAsync(userName).Result is not null)
                return;

            var user = new ApplicationUser
            {
                Id = id,
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var result = userManager.CreateAsync(user, password).Result;

            if (!result.Succeeded)
                throw new Exception($"Seed user '{userName}' thất bại: " +
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
