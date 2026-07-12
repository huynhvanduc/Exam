using Exam.Application.CategoryAggregate.Commands.CreateCategory;
using Exam.Application.CategoryAggregate.Commands.DeleteCategory;
using Exam.Application.CategoryAggregate.Commands.UpdateCategory;
using Exam.Application.CategoryAggregate.Queries.GetCategories;
using Exam.Application.CategoryAggregate.Queries.GetCategoryById;
using Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;
using Exam.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCategoryCommand(request.Name, request.UrlPath), cancellationToken);
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
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Update(string id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, request.Name, request.UrlPath), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
