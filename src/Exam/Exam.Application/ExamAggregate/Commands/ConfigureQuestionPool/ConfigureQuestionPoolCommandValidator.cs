using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ConfigureQuestionPool;

public class ConfigureQuestionPoolCommandValidator : AbstractValidator<ConfigureQuestionPoolCommand>
{
    public ConfigureQuestionPoolCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.PoolCategoryId).NotEmpty();
        RuleFor(x => x.PoolQuestionCount).GreaterThan(0);
    }
}
