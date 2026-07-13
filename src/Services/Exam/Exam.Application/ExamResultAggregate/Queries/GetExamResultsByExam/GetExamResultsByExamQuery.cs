using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;

public record GetExamResultsByExamQuery(string ExamId, Actor Actor, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ExamResultAdminListItemDto>>;
