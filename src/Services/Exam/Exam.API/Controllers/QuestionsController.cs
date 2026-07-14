using Exam.API.Extensions;
using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Application.QuestionAggregate.Commands.DeleteQuestion;
using Exam.Application.QuestionAggregate.Commands.ImportQuestions;
using Exam.Application.QuestionAggregate.Commands.MoveQuestions;
using Exam.Application.QuestionAggregate.Commands.UpdateQuestion;
using Exam.Application.QuestionAggregate.Queries.ExportQuestions;
using Exam.Application.QuestionAggregate.Queries.GetQuestionById;
using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;
using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Question.Create)]
    public async Task<IActionResult> Create([FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateQuestionCommand(request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, User.GetUserId()!);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Question.View)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionByIdQuery(id), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("by-category/{categoryId}")]
    [Authorize(Policy = Permissions.Question.View)]
    public async Task<IActionResult> GetByCategory(string categoryId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionsByCategoryQuery(categoryId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-category/{categoryId}/page")]
    [Authorize(Policy = Permissions.Question.View)]
    public async Task<IActionResult> GetByCategoryPaged(string categoryId, [FromQuery] int page, [FromQuery] int pageSize,
        [FromQuery] Level? level, [FromQuery] QuestionType? questionType, [FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize) = PagingDefaults.Normalize(page, pageSize, 20);
        var query = new GetQuestionsByCategoryPagedQuery(categoryId, normalizedPage, normalizedPageSize, level, questionType, keyword);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Question.Update)]
    public async Task<IActionResult> Update(string id, [FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateQuestionCommand(id, request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, User.GetActor());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("move")]
    [Authorize(Policy = Permissions.Question.Update)]
    public async Task<IActionResult> Move([FromBody] MoveQuestionsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MoveQuestionsCommand(request.QuestionIds, request.TargetCategoryId, User.GetActor()), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Question.Delete)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteQuestionCommand(id, User.GetActor()), cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Policy = Permissions.Question.Create)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file, [FromQuery] bool dryRun, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest("File is required.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        var result = await _mediator.Send(new ImportQuestionsCommand(stream.ToArray(), User.GetUserId()!, dryRun), cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Policy = Permissions.Question.View)]
    public async Task<IActionResult> Export([FromQuery] string categoryId, CancellationToken cancellationToken)
    {
        var bytes = await _mediator.Send(new ExportQuestionsQuery(categoryId), cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cau-hoi.xlsx");
    }
}
