using MediatR;

namespace Exam.Application.CategoryAggregate.Queries.GetCategories;

public record GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>;
