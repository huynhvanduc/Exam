using Exam.Application.RoleAggregate.Queries.GetRolePermissions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;

namespace Exam.Application.UnitTests.RoleAggregate.Queries;

public class GetRolePermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoStoredSets_ReturnsDefaultsWithAdminHavingAllPermissions()
    {
        var repository = new Mock<IRolePermissionRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetRolePermissionsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRolePermissionsQuery(), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Empty(result.Single(r => r.Role == UserRole.Student).Permissions);
        Assert.Empty(result.Single(r => r.Role == UserRole.Instructor).Permissions);
        Assert.Equal(Exam.Contracts.Permissions.All.Count, result.Single(r => r.Role == UserRole.Admin).Permissions.Count);
    }

    [Fact]
    public async Task Handle_StoredSetsExist_UsesStoredPermissions()
    {
        var stored = new[] { new RolePermissionSet(UserRole.Instructor, ["Category.Create"]) };
        var repository = new Mock<IRolePermissionRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        var handler = new GetRolePermissionsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetRolePermissionsQuery(), CancellationToken.None);

        Assert.Contains("Category.Create", result.Single(r => r.Role == UserRole.Instructor).Permissions);
    }
}
