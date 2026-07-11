using FluentValidation;

namespace Exam.Application.CategoryAggregate.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UrlPath).NotEmpty().MaximumLength(200);
    }
}
