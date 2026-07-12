using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ConfigureNegativeMarking;

public class ConfigureNegativeMarkingCommandValidator : AbstractValidator<ConfigureNegativeMarkingCommand>
{
    public ConfigureNegativeMarkingCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.Ratio).InclusiveBetween(0m, 1m);
    }
}
