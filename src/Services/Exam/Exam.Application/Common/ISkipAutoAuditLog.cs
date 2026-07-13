namespace Exam.Application.Common;

// Marker cho command mà AuditLoggingBehavior KHÔNG nên tự ghi log: hoặc vì handler đã tự ghi
// log chi tiết hơn (vd: đổi role - cần mô tả "từ X sang Y"), hoặc vì tần suất gọi quá cao/không
// mang ý nghĩa nghiệp vụ đáng lưu vết (vd: EnsureUserProvisioned chạy trên mọi request).
public interface ISkipAutoAuditLog
{
}
