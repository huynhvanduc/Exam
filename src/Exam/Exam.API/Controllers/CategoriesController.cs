using Exam.Application.CategoryAggregate.Commands.CreateCategory;
using Exam.Application.CategoryAggregate.Commands.DeleteCategory;
using Exam.Application.CategoryAggregate.Commands.UpdateCategory;
using Exam.Application.CategoryAggregate.Queries.GetCategories;
using Exam.Application.CategoryAggregate.Queries.GetCategoryById;
using Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("by-url-path/{urlPath}")]
    public async Task<IActionResult> GetByUrlPath(string urlPath, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryByUrlPathQuery(urlPath), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { Id = id }, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
