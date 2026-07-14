using Exam.API.Extensions;
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
    public async Task<IActionResult> GetRecent([FromQuery] int page, [FromQuery] int pageSize,
        [FromQuery] string? actor, [FromQuery] string? action, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 50);
        var query = new GetAuditLogQuery(normalizedPage, normalizedPageSize, actor, action, from, to);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
