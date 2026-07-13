using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetAvailableExams;

public record GetAvailableExamsQuery(string UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ExamDto>>;
