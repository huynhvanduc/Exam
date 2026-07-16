namespace Exam.Domain.Services;

public record ProvisionedAccount(string ExternalId, string GeneratedPassword);

// Tạo tài khoản đăng nhập mới bên Identity.Server (nơi duy nhất giữ mật khẩu) - implementation thật
// (gọi HTTP sang Identity.Server) nằm ở Exam.Infrastructure, theo đúng pattern interface-ở-Domain /
// implementation-ở-Infrastructure đã dùng cho mọi repository trong codebase này.
public interface IIdentityAccountProvisioningService
{
    Task<ProvisionedAccount> CreateAccountAsync(string email, string firstName, string lastName, CancellationToken cancellationToken = default);
}
