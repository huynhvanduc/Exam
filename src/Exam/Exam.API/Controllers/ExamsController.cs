using Exam.Application.ExamAggregate.Commands.AddQuestionToExam;
using Exam.Application.ExamAggregate.Commands.ArchiveExam;
using Exam.Application.ExamAggregate.Commands.ConfigureNegativeMarking;
using Exam.Application.ExamAggregate.Commands.ConfigureQuestionPool;
using Exam.Application.ExamAggregate.Commands.CreateExam;
using Exam.Application.ExamAggregate.Commands.DeleteExam;
using Exam.Application.ExamAggregate.Commands.PublishExam;
using Exam.Application.ExamAggregate.Commands.RemoveQuestionFromExam;
using Exam.Application.ExamAggregate.Commands.ScheduleExamAvailability;
using Exam.Application.ExamAggregate.Commands.UnpublishExam;
using Exam.Application.ExamAggregate.Commands.UpdateExam;
using Exam.Application.ExamAggregate.Queries.GetExamById;
using Exam.Application.ExamAggregate.Queries.GetExamsByCategory;
using Exam.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExamCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamByIdQuery(id), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("by-category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(string categoryId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamsByCategoryQuery(categoryId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateExamRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateExamCommand(id, request.Name, request.ShortDesc, request.Content, request.Duration,
            request.Level, request.CategoryId, request.IsTimeRestricted, request.MinimumPassingScore);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteExamCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/questions/{questionId}")]
    public async Task<IActionResult> AddQuestion(string id, string questionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddQuestionToExamCommand(id, questionId), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/questions/{questionId}")]
    public async Task<IActionResult> RemoveQuestion(string id, string questionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveQuestionFromExamCommand(id, questionId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/question-pool")]
    public async Task<IActionResult> ConfigureQuestionPool(string id, [FromBody] ConfigureQuestionPoolRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureQuestionPoolCommand(id, request.PoolCategoryId, request.PoolQuestionCount);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/availability")]
    public async Task<IActionResult> ScheduleAvailability(string id, [FromBody] ScheduleExamAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var command = new ScheduleExamAvailabilityCommand(id, request.AvailableFrom, request.AvailableTo);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/negative-marking")]
    public async Task<IActionResult> ConfigureNegativeMarking(string id, [FromBody] ConfigureNegativeMarkingRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureNegativeMarkingCommand(id, request.Ratio);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> Publish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PublishExamCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/unpublish")]
    public async Task<IActionResult> Unpublish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UnpublishExamCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveExamCommand(id), cancellationToken);
        return Ok(result);
    }
}

public record UpdateExamRequest(
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    int MinimumPassingScore);

public record ConfigureQuestionPoolRequest(string PoolCategoryId, int PoolQuestionCount);

public record ScheduleExamAvailabilityRequest(DateTime? AvailableFrom, DateTime? AvailableTo);

public record ConfigureNegativeMarkingRequest(decimal Ratio);
