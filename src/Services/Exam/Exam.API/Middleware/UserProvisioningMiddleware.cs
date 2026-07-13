using System.Security.Claims;
using Exam.API.Extensions;
using Exam.Application.UserAggregate.Commands.EnsureUserProvisioned;
using MediatR;

namespace Exam.API.Middleware;

public class UserProvisioningMiddleware
{
    private readonly RequestDelegate _next;

    public UserProvisioningMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMediator mediator)
    {
        var userId = context.User.GetUserId();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var email = context.User.FindFirst("email")?.Value ?? string.Empty;
            var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
            var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;

            var user = await mediator.Send(new EnsureUserProvisionedCommand(userId, email, firstName, lastName), context.RequestAborted);

            // Role sống trong Exam.Domain.User (không phải claim JWT do Identity Server phát hành),
            // nên gắn thêm vào ClaimsPrincipal ngay sau khi provision để [Authorize(Roles=...)] dùng được.
            if (context.User.Identity is ClaimsIdentity identity)
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        }

        await _next(context);
    }
}
