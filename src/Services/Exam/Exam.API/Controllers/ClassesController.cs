using Exam.API.Extensions;
using Exam.Application.ClassAggregate.Commands.CreateClassRoom;
using Exam.Application.ClassAggregate.Commands.DeleteClassRoom;
using Exam.Application.ClassAggregate.Commands.JoinClass;
using Exam.Application.ClassAggregate.Commands.RegenerateJoinCode;
using Exam.Application.ClassAggregate.Commands.RemoveMember;
using Exam.Application.ClassAggregate.Commands.RenameClassRoom;
using Exam.Application.ClassAggregate.Queries.GetAllClassRooms;
using Exam.Application.ClassAggregate.Queries.GetClassRoomById;
using Exam.Application.ClassAggregate.Queries.GetMyClassRooms;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Class.Create)]
    public async Task<IActionResult> Create([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateClassRoomCommand(request.Name, User.GetUserId()!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Class.View)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllClassRoomsQuery(User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyClassRoomsQuery(User.GetUserId()!), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Class.View)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClassRoomByIdQuery(id, User.GetActor()), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Class.Update)]
    public async Task<IActionResult> Rename(string id, [FromBody] RenameClassRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RenameClassRoomCommand(id, request.Name, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Class.Delete)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteClassRoomCommand(id, User.GetActor()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/regenerate-code")]
    [Authorize(Policy = Permissions.Class.Update)]
    public async Task<IActionResult> RegenerateCode(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegenerateJoinCodeCommand(id, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/members/{userId}")]
    [Authorize(Policy = Permissions.Class.ManageMembers)]
    public async Task<IActionResult> RemoveMember(string id, string userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveMemberCommand(id, userId, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinClassRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new JoinClassCommand(request.JoinCode, User.GetUserId()!), cancellationToken);
        return Ok(result);
    }
}
