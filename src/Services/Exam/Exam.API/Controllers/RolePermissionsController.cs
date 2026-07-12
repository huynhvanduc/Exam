using Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;
using Exam.Application.RoleAggregate.Queries.GetRolePermissions;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/role-permissions")]
[Authorize(Roles = "Admin")]
public class RolePermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolePermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRolePermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{role}")]
    public async Task<IActionResult> Update(UserRole role, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateRolePermissionsCommand(role, request.Permissions), cancellationToken);
        return Ok(result);
    }
}
