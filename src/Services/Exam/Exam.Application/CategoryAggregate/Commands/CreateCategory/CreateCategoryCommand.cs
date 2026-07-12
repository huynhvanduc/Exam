using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string UrlPath) : IRequest<CategoryDto>;
