using Exam.Application.Common;
using Exam.Application.RoleAggregate.Commands.UpdateRolePermissions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.RoleAggregate.Commands;

public class UpdateRolePermissionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_AdminRole_ThrowsDomainException()
    {
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new UpdateRolePermissionsCommandHandler(rolePermissionRepository.Object, auditLogRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new UpdateRolePermissionsCommand(UserRole.Admin, ["Category.Create"], new Actor("admin-1", UserRole.Admin)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoExistingSet_InsertsNewSet()
    {
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository.Setup(r => r.GetByRoleAsync(UserRole.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolePermissionSet)null!);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new UpdateRolePermissionsCommandHandler(rolePermissionRepository.Object, auditLogRepository.Object);

        var result = await handler.Handle(
            new UpdateRolePermissionsCommand(UserRole.Instructor, ["Category.Create"], new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.Equal(UserRole.Instructor, result.Role);
        Assert.Contains("Category.Create", result.Permissions);
        rolePermissionRepository.Verify(r => r.InsertAsync(It.IsAny<RolePermissionSet>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingSet_ReplacesPermissions()
    {
        var existing = new RolePermissionSet(UserRole.Instructor, ["Category.Create"]);
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository.Setup(r => r.GetByRoleAsync(UserRole.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new UpdateRolePermissionsCommandHandler(rolePermissionRepository.Object, auditLogRepository.Object);

        var result = await handler.Handle(
            new UpdateRolePermissionsCommand(UserRole.Instructor, ["Question.View"], new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.Contains("Question.View", result.Permissions);
        Assert.DoesNotContain("Category.Create", result.Permissions);
        rolePermissionRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionsChanged_WritesAuditLogWithGrantedAndRevoked()
    {
        var existing = new RolePermissionSet(UserRole.Instructor, ["Category.Create"]);
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository.Setup(r => r.GetByRoleAsync(UserRole.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        AuditLogEntry? capturedEntry = null;
        auditLogRepository.Setup(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .Returns(Task.CompletedTask);
        var handler = new UpdateRolePermissionsCommandHandler(rolePermissionRepository.Object, auditLogRepository.Object);

        await handler.Handle(
            new UpdateRolePermissionsCommand(UserRole.Instructor, ["Question.View"], new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.NotNull(capturedEntry);
        Assert.Equal("Role.PermissionsChanged", capturedEntry!.Action);
        Assert.Contains("Question.View", capturedEntry.Description);
        Assert.Contains("Category.Create", capturedEntry.Description);
    }

    [Fact]
    public async Task Handle_NoPermissionsChanged_DoesNotWriteAuditLog()
    {
        var existing = new RolePermissionSet(UserRole.Instructor, ["Category.Create"]);
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        rolePermissionRepository.Setup(r => r.GetByRoleAsync(UserRole.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new UpdateRolePermissionsCommandHandler(rolePermissionRepository.Object, auditLogRepository.Object);

        await handler.Handle(
            new UpdateRolePermissionsCommand(UserRole.Instructor, ["Category.Create"], new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        auditLogRepository.Verify(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class UpdateRolePermissionsCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UpdateRolePermissionsCommandValidator.csv",
        s => s.Split(';', StringSplitOptions.RemoveEmptyEntries), CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string[] permissions, bool isValid)
    {
        var validator = new UpdateRolePermissionsCommandValidator();

        var result = validator.Validate(new UpdateRolePermissionsCommand(UserRole.Instructor, permissions, new Actor("admin-1", UserRole.Admin)));

        Assert.Equal(isValid, result.IsValid);
    }
}
