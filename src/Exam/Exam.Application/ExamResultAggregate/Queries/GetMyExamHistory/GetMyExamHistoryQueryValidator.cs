using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;

public class GetMyExamHistoryQueryValidator : AbstractValidator<GetMyExamHistoryQuery>
{
    public GetMyExamHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
