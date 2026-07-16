using Exam.Application.Common;

namespace Exam.Infrastructure.IntegrationTest;

// AuditLoggingBehavior đọc UserId từ đây để ghi ActorUserId - set trước khi Send() nếu test cần
// kiểm tra nội dung audit log; để null (mặc định) nếu không quan tâm ai là actor.
public class TestCurrentUserAccessor : ICurrentUserAccessor
{
    public string? UserId { get; set; }
}
