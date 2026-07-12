using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetAvailableExams;

public record GetAvailableExamsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ExamDto>>;
