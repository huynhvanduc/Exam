using MediatR;

namespace Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;

public record GetCategoryByUrlPathQuery(string UrlPath) : IRequest<CategoryDto?>;
