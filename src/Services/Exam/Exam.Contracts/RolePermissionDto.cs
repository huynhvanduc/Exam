namespace Exam.Contracts;

public record RolePermissionDto(UserRole Role, IReadOnlyCollection<string> Permissions);

public record UpdateRolePermissionsRequest(IReadOnlyCollection<string> Permissions);
