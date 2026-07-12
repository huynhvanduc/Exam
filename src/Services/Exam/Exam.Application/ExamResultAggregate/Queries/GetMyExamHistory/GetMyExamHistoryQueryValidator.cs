using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public class GetMyExamHistoryQueryValidator : AbstractValidator<GetMyExamHistoryQuery>
{
    public GetMyExamHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
