using Exam.Application.AuditAggregate.Queries.GetAuditLog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int limit, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(limit), cancellationToken);
        return Ok(result);
    }
}
