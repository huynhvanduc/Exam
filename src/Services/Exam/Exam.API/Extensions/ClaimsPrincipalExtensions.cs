using System.Security.Claims;
using Exam.Application.Common;
using Exam.Contracts;

namespace Exam.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) => user.FindFirstValue("sub");

    public static UserRole GetUserRole(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.Student;

    public static Actor GetActor(this ClaimsPrincipal user) => new(user.GetUserId()!, user.GetUserRole());
}
