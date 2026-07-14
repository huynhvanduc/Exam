using MediatR;

namespace Exam.Application.AuditAggregate.Queries.GetAuditLog;

public record GetAuditLogQuery(int Page = 1, int PageSize = 50, string? Actor = null, string? Action = null,
    DateTime? From = null, DateTime? To = null) : IRequest<PagedResult<AuditLogEntryDto>>;
