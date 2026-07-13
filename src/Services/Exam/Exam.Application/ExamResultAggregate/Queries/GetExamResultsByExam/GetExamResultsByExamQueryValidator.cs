using FluentValidation;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;

public class GetExamResultsByExamQueryValidator : AbstractValidator<GetExamResultsByExamQuery>
{
    public GetExamResultsByExamQueryValidator()
    {
        RuleFor(x => x.ExamId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
