using MediatR;

namespace Exam.Application.AuditAggregate.Queries.GetAuditLog;

public record GetAuditLogQuery(int Page = 1, int PageSize = 50) : IRequest<PagedResult<AuditLogEntryDto>>;
