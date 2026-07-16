using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.UserAggregate.Commands.PromoteUserRole;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.UserAggregate.Commands;

public class PromoteUserRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        var handler = new PromoteUserRoleCommandHandler(userRepository.Object, new Mock<IAuditLogRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PromoteUserRoleCommand("ext-1", UserRole.Instructor, new Actor("admin-1", UserRole.Admin)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActorPromotesSelf_ThrowsForbiddenException()
    {
        var user = new User("admin-1", "user@example.com", "Nguyễn", "Văn A");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("admin-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new PromoteUserRoleCommandHandler(userRepository.Object, new Mock<IAuditLogRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new PromoteUserRoleCommand("admin-1", UserRole.Instructor, new Actor("admin-1", UserRole.Admin)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ChangesRoleAndWritesAuditLog()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A", UserRole.Student);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        AuditLogEntry? capturedEntry = null;
        auditLogRepository.Setup(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .Returns(Task.CompletedTask);
        var handler = new PromoteUserRoleCommandHandler(userRepository.Object, auditLogRepository.Object);

        var result = await handler.Handle(new PromoteUserRoleCommand("ext-1", UserRole.Instructor, new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.Equal(UserRole.Instructor, result.Role);
        userRepository.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(capturedEntry);
        Assert.Contains("Student", capturedEntry!.Description);
        Assert.Contains("Instructor", capturedEntry.Description);
    }
}

public class PromoteUserRoleCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("PromoteUserRoleCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string externalId, bool isValid)
    {
        var validator = new PromoteUserRoleCommandValidator();

        var result = validator.Validate(new PromoteUserRoleCommand(externalId, UserRole.Instructor, new Actor("admin-1", UserRole.Admin)));

        Assert.Equal(isValid, result.IsValid);
    }
}
