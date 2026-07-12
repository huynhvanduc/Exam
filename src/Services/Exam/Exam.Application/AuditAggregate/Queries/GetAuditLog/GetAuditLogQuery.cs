using MediatR;

namespace Exam.Application.AuditAggregate.Queries.GetAuditLog;

public record GetAuditLogQuery(int Limit) : IRequest<IReadOnlyCollection<AuditLogEntryDto>>;
