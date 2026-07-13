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
using Exam.API.Extensions;
using Exam.Application.ExamAggregate.Queries.GetAvailableExams;
using Exam.Application.ExamAggregate.Queries.GetExamById;
using Exam.Application.ExamAggregate.Queries.GetExamsByCategory;
using Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = Permissions.Exam.Create)]
    public async Task<IActionResult> Create([FromBody] ExamRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateExamCommand(request.Name, request.ShortDesc, request.Content, request.Duration,
            request.Level, request.CategoryId, request.IsTimeRestricted, request.MinimumPassingScore, User.GetUserId()!);
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
    public async Task<IActionResult> GetByCategory(string categoryId, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var query = new GetExamsByCategoryQuery(categoryId, normalizedPage, normalizedPageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var query = new GetAvailableExamsQuery(normalizedPage, normalizedPageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Exam.Update)]
    public async Task<IActionResult> Update(string id, [FromBody] ExamRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateExamCommand(id, request.Name, request.ShortDesc, request.Content, request.Duration,
            request.Level, request.CategoryId, request.IsTimeRestricted, request.MinimumPassingScore, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Exam.Delete)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteExamCommand(id, User.GetActor()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/questions/{questionId}")]
    [Authorize(Policy = Permissions.Exam.ManageQuestions)]
    public async Task<IActionResult> AddQuestion(string id, string questionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddQuestionToExamCommand(id, questionId, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/questions/{questionId}")]
    [Authorize(Policy = Permissions.Exam.ManageQuestions)]
    public async Task<IActionResult> RemoveQuestion(string id, string questionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveQuestionFromExamCommand(id, questionId, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/question-pool")]
    [Authorize(Policy = Permissions.Exam.ManagePool)]
    public async Task<IActionResult> ConfigureQuestionPool(string id, [FromBody] ConfigureQuestionPoolRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureQuestionPoolCommand(id, request.PoolCategoryId, request.PoolQuestionCount, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/availability")]
    [Authorize(Policy = Permissions.Exam.ManageAvailability)]
    public async Task<IActionResult> ScheduleAvailability(string id, [FromBody] ScheduleExamAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var command = new ScheduleExamAvailabilityCommand(id, request.AvailableFrom, request.AvailableTo, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/negative-marking")]
    [Authorize(Policy = Permissions.Exam.ManageNegativeMarking)]
    public async Task<IActionResult> ConfigureNegativeMarking(string id, [FromBody] ConfigureNegativeMarkingRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfigureNegativeMarkingCommand(id, request.Ratio, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/publish")]
    [Authorize(Policy = Permissions.Exam.Publish)]
    public async Task<IActionResult> Publish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PublishExamCommand(id, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/unpublish")]
    [Authorize(Policy = Permissions.Exam.Unpublish)]
    public async Task<IActionResult> Unpublish(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UnpublishExamCommand(id, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/archive")]
    [Authorize(Policy = Permissions.Exam.Archive)]
    public async Task<IActionResult> Archive(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ArchiveExamCommand(id, User.GetActor()), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/results")]
    [Authorize(Policy = Permissions.Exam.ViewResults)]
    public async Task<IActionResult> GetResults(string id, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var result = await _mediator.Send(new GetExamResultsByExamQuery(id, normalizedPage, normalizedPageSize), cancellationToken);
        return Ok(result);
    }
}
