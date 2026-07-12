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
            var firstName = context.User.FindFirst("given_name")?.Value ?? string.Empty;
            var lastName = context.User.FindFirst("family_name")?.Value ?? string.Empty;

            await mediator.Send(new EnsureUserProvisionedCommand(userId, firstName, lastName), context.RequestAborted);
        }

        await _next(context);
    }
}
