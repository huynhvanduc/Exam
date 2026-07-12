using Exam.Domain.AggregateModels.AuditAggregate;
using MediatR;

namespace Exam.Application.AuditAggregate.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken) =>
        PagedResultFactory.CreateAsync<AuditLogEntryDto>(request.Page, request.PageSize,
            async (skip, take) => (await _auditLogRepository.GetPagedAsync(skip, take, cancellationToken))
                .Select(e => new AuditLogEntryDto(e.Id, e.Timestamp, e.ActorUserId, e.Action, e.TargetId, e.Description))
                .ToList(),
            () => _auditLogRepository.CountAsync(cancellationToken));
}
