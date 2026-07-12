using MediatR;

namespace Exam.Application.CategoryAggregate.Queries.GetCategoryById;

public record GetCategoryByIdQuery(string Id) : IRequest<CategoryDto?>;
