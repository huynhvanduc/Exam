namespace Exam.Contracts;

public record AuditLogEntryDto(
    string Id,
    DateTime Timestamp,
    string ActorUserId,
    string Action,
    string TargetId,
    string Description);
