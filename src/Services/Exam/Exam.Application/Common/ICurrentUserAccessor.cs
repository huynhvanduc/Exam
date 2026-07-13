namespace Exam.Application.Common;

public interface ICurrentUserAccessor
{
    string? UserId { get; }
}
