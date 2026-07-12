using FluentValidation;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;

public class GetQuestionsByCategoryPagedQueryValidator : AbstractValidator<GetQuestionsByCategoryPagedQuery>
{
    public GetQuestionsByCategoryPagedQueryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
