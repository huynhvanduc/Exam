using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;

public record GetQuestionsByCategoryQuery(string CategoryId) : IRequest<IReadOnlyCollection<QuestionDto>>;
