using Exam.API.Extensions;
using Exam.Application.ExamResultAggregate.Commands.FinishExam;
using Exam.Application.ExamResultAggregate.Commands.RecordAnswer;
using Exam.Application.ExamResultAggregate.Commands.StartExam;
using Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;
using Exam.Application.ExamResultAggregate.Queries.GetExamResultById;
using Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;
using MediatR;
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

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyExamHistoryQuery(User.GetUserId()!), cancellationToken);
        return Ok(result);
    }
}

public record StartExamRequest(string ExamId);

public record RecordAnswerRequest(string QuestionId, IReadOnlyCollection<string> SelectedAnswerIds);
