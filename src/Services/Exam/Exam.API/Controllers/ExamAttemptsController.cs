using Exam.API.Extensions;
using Exam.Application.ExamResultAggregate.Commands.AdminForceFinishExam;
using Exam.Application.ExamResultAggregate.Commands.FinishExam;
using Exam.Application.ExamResultAggregate.Commands.RecordAnswer;
using Exam.Application.ExamResultAggregate.Commands.StartExam;
using Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;
using Exam.Application.ExamResultAggregate.Queries.GetExamAttemptAdminStatus;
using Exam.Application.ExamResultAggregate.Queries.GetExamResultById;
using Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/exam-attempts")]
public class ExamAttemptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExamAttemptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartExamRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartExamCommand(request.ExamId, User.GetUserId()!), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamAttemptQuery(id, User.GetUserId()!), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/answers")]
    public async Task<IActionResult> RecordAnswer(string id, [FromBody] RecordAnswerRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordAnswerCommand(id, User.GetUserId()!, request.QuestionId, request.SelectedAnswerIds);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/finish")]
    public async Task<IActionResult> Finish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FinishExamCommand(id, User.GetUserId()!), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/result")]
    public async Task<IActionResult> GetResult(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamResultByIdQuery(id, User.GetUserId()!), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{id}/admin-status")]
    [Authorize(Policy = Permissions.Exam.ViewResults)]
    public async Task<IActionResult> GetAdminStatus(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamAttemptAdminStatusQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/admin-force-finish")]
    [Authorize(Policy = Permissions.Exam.ForceFinishAttempt)]
    public async Task<IActionResult> AdminForceFinish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdminForceFinishExamCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var query = new GetMyExamHistoryQuery(User.GetUserId()!, normalizedPage, normalizedPageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
