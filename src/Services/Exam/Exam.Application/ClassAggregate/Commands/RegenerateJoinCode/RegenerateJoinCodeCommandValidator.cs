using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.RegenerateJoinCode;

public class RegenerateJoinCodeCommandValidator : AbstractValidator<RegenerateJoinCodeCommand>
{
    public RegenerateJoinCodeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
