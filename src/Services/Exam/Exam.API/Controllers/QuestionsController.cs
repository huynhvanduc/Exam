using Exam.API.Extensions;
using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Application.QuestionAggregate.Commands.DeleteQuestion;
using Exam.Application.QuestionAggregate.Commands.UpdateQuestion;
using Exam.Application.QuestionAggregate.Queries.GetQuestionById;
using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor,Admin")]
public class QuestionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateQuestionCommand(request.Content, request.QuestionType, request.Level,
            request.CategoryId, request.Answers, request.Explain, request.Points, User.GetUserId()!);
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
    public async Task<IActionResult> Update(string id, [FromBody] QuestionRequest request, CancellationToken cancellationToken)
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
