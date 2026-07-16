using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.UserAggregate.Commands.ResetUserPassword;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.UserAggregate.Commands;

public class ResetUserPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        var handler = new ResetUserPasswordCommandHandler(userRepository.Object, new Mock<IIdentityAccountProvisioningService>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ResetUserPasswordCommand("ext-1", new Actor("admin-1", UserRole.Admin)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActorResetsOwnPassword_ThrowsForbiddenException()
    {
        var user = new User("admin-1", "user@example.com", "Nguyễn", "Văn A");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("admin-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new ResetUserPasswordCommandHandler(userRepository.Object, new Mock<IIdentityAccountProvisioningService>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ResetUserPasswordCommand("admin-1", new Actor("admin-1", UserRole.Admin)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsGeneratedPassword()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var provisioningService = new Mock<IIdentityAccountProvisioningService>();
        provisioningService.Setup(s => s.ResetPasswordAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync("NewP@ss1");
        var handler = new ResetUserPasswordCommandHandler(userRepository.Object, provisioningService.Object);

        var result = await handler.Handle(new ResetUserPasswordCommand("ext-1", new Actor("admin-1", UserRole.Admin)), CancellationToken.None);

        Assert.Equal("ext-1", result.ExternalId);
        Assert.Equal("NewP@ss1", result.GeneratedPassword);
    }
}
