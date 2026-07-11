using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.DeleteCategory;

public record DeleteCategoryCommand(string Id) : IRequest;
