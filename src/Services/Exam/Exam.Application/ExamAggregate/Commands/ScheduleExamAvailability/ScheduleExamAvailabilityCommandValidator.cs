using FluentValidation;

namespace Exam.Application.ExamAggregate.Commands.ScheduleExamAvailability;

public class ScheduleExamAvailabilityCommandValidator : AbstractValidator<ScheduleExamAvailabilityCommand>
{
    public ScheduleExamAvailabilityCommandValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();

        RuleFor(x => x)
            .Must(x => !x.AvailableFrom.HasValue || !x.AvailableTo.HasValue || x.AvailableTo > x.AvailableFrom)
            .WithMessage("Available-to must be later than available-from.");
    }
}
