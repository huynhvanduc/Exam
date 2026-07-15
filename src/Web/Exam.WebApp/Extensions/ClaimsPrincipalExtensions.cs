using System.Security.Claims;

namespace Exam.WebApp.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetDefaultLandingPath(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return "/account/login";

        if (user.IsInRole("Admin"))
            return "/admin/dashboard";

        if (user.IsInRole("Instructor"))
            return "/admin/categories";

        return "/exams";
    }
}
