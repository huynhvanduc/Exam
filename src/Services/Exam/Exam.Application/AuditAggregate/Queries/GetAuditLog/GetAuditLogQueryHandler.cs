using Exam.Domain.AggregateModels.AuditAggregate;
using MediatR;

namespace Exam.Application.AuditAggregate.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, IReadOnlyCollection<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IReadOnlyCollection<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var limit = request.Limit <= 0 ? 100 : request.Limit;
        var entries = await _auditLogRepository.GetRecentAsync(limit, cancellationToken);

        return entries
            .Select(e => new AuditLogEntryDto(e.Id, e.Timestamp, e.ActorUserId, e.Action, e.TargetId, e.Description))
            .ToList();
    }
}
