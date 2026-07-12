using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.RoleAggregate;

public class RolePermissionSet : Entity, IAggregateRoot
{
    private HashSet<string> _permissions = new();

    public UserRole Role { get; private set; }

    public IReadOnlyCollection<string> Permissions
    {
        get => _permissions;
        private set => _permissions = value?.ToHashSet() ?? new HashSet<string>();
    }

    private RolePermissionSet()
    {
    }

    public RolePermissionSet(UserRole role, IReadOnlyCollection<string> permissions)
    {
        if (role == UserRole.Admin)
            throw new ExamDomainException("Admin always has full access and cannot have a managed permission set.");

        Role = role;
        _permissions = (permissions ?? []).ToHashSet();
    }

    public void ReplacePermissions(IEnumerable<string> permissions)
    {
        _permissions = (permissions ?? []).ToHashSet();
    }

    public bool Has(string permission) => _permissions.Contains(permission);
}
