using System.Security.Claims;

namespace Exam.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirstValue("sub");
}
