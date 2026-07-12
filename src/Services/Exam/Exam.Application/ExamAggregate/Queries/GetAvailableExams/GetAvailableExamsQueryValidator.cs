using FluentValidation;

namespace Exam.Application.ExamAggregate.Queries.GetAvailableExams;

public class GetAvailableExamsQueryValidator : AbstractValidator<GetAvailableExamsQuery>
{
    public GetAvailableExamsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
