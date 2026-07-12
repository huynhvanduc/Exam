using Exam.API.Extensions;
using Exam.Application.UserAggregate.Commands.PromoteUserRole;
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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{externalId}/role")]
    [Authorize(Policy = Permissions.User.PromoteRole)]
    public async Task<IActionResult> PromoteRole(string externalId, [FromBody] PromoteUserRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PromoteUserRoleCommand(externalId, request.Role, User.GetActor()), cancellationToken);
        return Ok(result);
    }
}
