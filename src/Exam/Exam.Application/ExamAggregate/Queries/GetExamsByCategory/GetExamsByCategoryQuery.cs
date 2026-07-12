using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamsByCategory;

public record GetExamsByCategoryQuery(string CategoryId) : IRequest<IReadOnlyCollection<ExamDto>>;
