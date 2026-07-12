using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.UpdateCategory;

public record UpdateCategoryCommand(string Id, string Name, string UrlPath) : IRequest<CategoryDto>;
