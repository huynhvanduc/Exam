using System.Security.Claims;
using Exam.API.Extensions;
using Exam.Application.Exceptions;
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

            // IsActive sống trong Exam.Domain.User, JWT của Identity Server không biết gì về trạng thái
            // khoá này -> phải tự chặn ở đây cho MỌI request đã xác thực, không chỉ riêng action nhạy cảm.
            if (!user.IsActive)
                throw new ForbiddenException("Tài khoản của bạn đã bị khóa. Liên hệ quản trị viên để được hỗ trợ.");

            // Role sống trong Exam.Domain.User (không phải claim JWT do Identity Server phát hành),
            // nên gắn thêm vào ClaimsPrincipal ngay sau khi provision để [Authorize(Roles=...)] dùng được.
            if (context.User.Identity is ClaimsIdentity identity)
                identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        }

        await _next(context);
    }
}
