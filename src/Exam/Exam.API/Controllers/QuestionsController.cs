using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Application.QuestionAggregate.Commands.DeleteQuestion;
using Exam.Application.QuestionAggregate.Commands.UpdateQuestion;
using Exam.Application.QuestionAggregate.Queries.GetQuestionById;
using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;
using Exam.Domain.Enums;
using MediatR;
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
    public async Task<IActionResult> Create([FromBody] CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionByIdQuery(id), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("by-category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(string categoryId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionsByCategoryQuery(categoryId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateQuestionCommand(id, request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, request.Points);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteQuestionCommand(id), cancellationToken);
        return NoContent();
    }
}

public record UpdateQuestionRequest(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    int Points);
