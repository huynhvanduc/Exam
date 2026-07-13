using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;

public class ConfigureMaxAttemptsCommandValidator : AbstractValidator<ConfigureMaxAttemptsCommand>
{
    public ConfigureMaxAttemptsCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.MaxAttempts).GreaterThan(0).When(x => x.MaxAttempts.HasValue);
    }
}
