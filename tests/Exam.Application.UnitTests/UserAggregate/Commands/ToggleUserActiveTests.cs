using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.UserAggregate.Commands.ToggleUserActive;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.UserAggregate.Commands;

public class ToggleUserActiveCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        var handler = new ToggleUserActiveCommandHandler(userRepository.Object, new Mock<IAuditLogRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ToggleUserActiveCommand("ext-1", false, new Actor("admin-1", UserRole.Admin)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActorLocksSelf_ThrowsForbiddenException()
    {
        var user = new User("admin-1", "user@example.com", "Nguyễn", "Văn A");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("admin-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new ToggleUserActiveCommandHandler(userRepository.Object, new Mock<IAuditLogRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ToggleUserActiveCommand("admin-1", false, new Actor("admin-1", UserRole.Admin)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Deactivate_SetsIsActiveFalseAndWritesAuditLog()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new ToggleUserActiveCommandHandler(userRepository.Object, auditLogRepository.Object);

        var result = await handler.Handle(new ToggleUserActiveCommand("ext-1", false, new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.False(result.IsActive);
        auditLogRepository.Verify(r => r.InsertAsync(It.Is<AuditLogEntry>(e => e.Description == "Locked account."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Activate_SetsIsActiveTrueAndWritesAuditLog()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        user.Deactivate();
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new ToggleUserActiveCommandHandler(userRepository.Object, auditLogRepository.Object);

        var result = await handler.Handle(new ToggleUserActiveCommand("ext-1", true, new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.True(result.IsActive);
        auditLogRepository.Verify(r => r.InsertAsync(It.Is<AuditLogEntry>(e => e.Description == "Unlocked account."),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ToggleUserActiveCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("ToggleUserActiveCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string externalId, bool isValid)
    {
        var validator = new ToggleUserActiveCommandValidator();

        var result = validator.Validate(new ToggleUserActiveCommand(externalId, true, new Actor("admin-1", UserRole.Admin)));

        Assert.Equal(isValid, result.IsValid);
    }
}
