using System.Security.Claims;
using Identity.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Identity.Server.Services;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public ApplicationUserClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // IdentityServer4.AspNetIdentity chỉ đưa vào token đúng claim type đã có trên identity này -
        // UserClaimsPrincipalFactory mặc định không map FirstName/LastName sang given_name/family_name,
        // và ClaimTypes.Email không trùng short-form "email" mà OIDC/UserClaims của ApiResource yêu cầu.
        identity.AddClaim(new Claim("given_name", user.FirstName ?? string.Empty));
        identity.AddClaim(new Claim("family_name", user.LastName ?? string.Empty));
        identity.AddClaim(new Claim("email", user.Email ?? string.Empty));
        identity.AddClaim(new Claim("name", $"{user.FirstName} {user.LastName}".Trim()));

        return identity;
    }
}
