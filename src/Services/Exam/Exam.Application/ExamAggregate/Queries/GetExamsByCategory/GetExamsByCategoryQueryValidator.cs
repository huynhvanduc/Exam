using FluentValidation;

namespace Exam.Application.ExamAggregate.Queries.GetExamsByCategory;

public class GetExamsByCategoryQueryValidator : AbstractValidator<GetExamsByCategoryQuery>
{
    public GetExamsByCategoryQueryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
