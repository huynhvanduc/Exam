using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.AuditAggregate;

public class AuditLogEntry : Entity, IAggregateRoot
{
    public DateTime Timestamp { get; private set; }

    public string ActorUserId { get; private set; }

    public string Action { get; private set; }

    public string TargetId { get; private set; }

    public string Description { get; private set; }

    private AuditLogEntry()
    {
    }

    public AuditLogEntry(string actorUserId, string action, string targetId, string description)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            throw new ExamDomainException("Audit log actor is required.");

        if (string.IsNullOrWhiteSpace(action))
            throw new ExamDomainException("Audit log action is required.");

        Timestamp = DateTime.UtcNow;
        ActorUserId = actorUserId;
        Action = action;
        TargetId = targetId;
        Description = description;
    }
}
