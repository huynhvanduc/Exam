using Exam.Application.UserAggregate.Commands.EnsureUserProvisioned;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.UserAggregate.Commands;

public class EnsureUserProvisionedCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUser_ReturnsExistingWithoutInserting()
    {
        var existing = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var handler = new EnsureUserProvisionedCommandHandler(repository.Object);

        var result = await handler.Handle(new EnsureUserProvisionedCommand("ext-1", "user@example.com", "Nguyễn", "Văn A"),
            CancellationToken.None);

        Assert.Equal("ext-1", result.ExternalId);
        repository.Verify(r => r.InsertAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NewUser_FirstUserInSystem_BecomesAdmin()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        repository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new EnsureUserProvisionedCommandHandler(repository.Object);

        var result = await handler.Handle(new EnsureUserProvisionedCommand("ext-1", "user@example.com", "Nguyễn", "Văn A"),
            CancellationToken.None);

        Assert.Equal(UserRole.Admin, result.Role);
        repository.Verify(r => r.InsertAsync(It.Is<User>(u => u.Role == UserRole.Admin), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NewUser_NotFirstUser_BecomesStudent()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        repository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new EnsureUserProvisionedCommandHandler(repository.Object);

        var result = await handler.Handle(new EnsureUserProvisionedCommand("ext-1", "user@example.com", "Nguyễn", "Văn A"),
            CancellationToken.None);

        Assert.Equal(UserRole.Student, result.Role);
    }

    [Fact]
    public async Task Handle_BlankFirstAndLastName_FallsBackToDefaults()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        repository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new EnsureUserProvisionedCommandHandler(repository.Object);

        var result = await handler.Handle(new EnsureUserProvisionedCommand("ext-1", "user@example.com", "", ""),
            CancellationToken.None);

        Assert.Equal("Unknown", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    [Fact]
    public async Task Handle_NullEmail_BecomesEmptyString()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        repository.Setup(r => r.AnyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new EnsureUserProvisionedCommandHandler(repository.Object);

        var result = await handler.Handle(new EnsureUserProvisionedCommand("ext-1", null!, "Nguyễn", "Văn A"),
            CancellationToken.None);

        Assert.Equal(string.Empty, result.Email);
    }
}
