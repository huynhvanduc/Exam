using Exam.API.Extensions;
using Exam.Application.UserAggregate.Commands.CreateUser;
using Exam.Application.UserAggregate.Commands.PromoteUserRole;
using Exam.Application.UserAggregate.Commands.ResetUserPassword;
using Exam.Application.UserAggregate.Commands.ToggleUserActive;
using Exam.Application.UserAggregate.Queries.GetUserByExternalId;
using Exam.Application.UserAggregate.Queries.GetUsers;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByExternalIdQuery(User.GetUserId()!), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.User.View)]
    public async Task<IActionResult> GetAll([FromQuery] int page, [FromQuery] int pageSize,
        [FromQuery] string? search, [FromQuery] UserRole? role, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var query = new GetUsersQuery(normalizedPage, normalizedPageSize, search, role, isActive);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.User.Create)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateUserCommand(request.Email, request.FirstName, request.LastName, request.Role, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{externalId}/reset-password")]
    [Authorize(Policy = Permissions.User.ResetPassword)]
    public async Task<IActionResult> ResetPassword(string externalId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetUserPasswordCommand(externalId, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{externalId}/role")]
    [Authorize(Policy = Permissions.User.PromoteRole)]
    public async Task<IActionResult> PromoteRole(string externalId, [FromBody] PromoteUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PromoteUserRoleCommand(externalId, request.Role, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{externalId}/active")]
    [Authorize(Policy = Permissions.User.ToggleActive)]
    public async Task<IActionResult> ToggleActive(string externalId, [FromBody] ToggleUserActiveRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleUserActiveCommand(externalId, request.IsActive, User.GetActor()), cancellationToken);
        return Ok(result);
    }
}
