using Exam.Application.Common;
using Exam.Application.UserAggregate.Commands.CreateUser;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.UserAggregate.Commands;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ProvisionsIdentityAccountAndInsertsUser()
    {
        var userRepository = new Mock<IUserRepository>();
        var provisioningService = new Mock<IIdentityAccountProvisioningService>();
        provisioningService.Setup(s => s.CreateAccountAsync("user@example.com", "Nguyễn", "Văn A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProvisionedAccount("ext-1", "GeneratedP@ss1"));
        var handler = new CreateUserCommandHandler(userRepository.Object, provisioningService.Object);

        var result = await handler.Handle(
            new CreateUserCommand("user@example.com", "Nguyễn", "Văn A", UserRole.Instructor, new Actor("admin-1", UserRole.Admin)),
            CancellationToken.None);

        Assert.Equal("GeneratedP@ss1", result.GeneratedPassword);
        Assert.Equal("ext-1", result.User.ExternalId);
        Assert.Equal(UserRole.Instructor, result.User.Role);
        userRepository.Verify(r => r.InsertAsync(It.Is<User>(u => u.ExternalId == "ext-1" && u.Role == UserRole.Instructor),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CreateUserCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("CreateUserCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string email, string firstName, string lastName, bool isValid)
    {
        var validator = new CreateUserCommandValidator();

        var result = validator.Validate(new CreateUserCommand(email, firstName, lastName, UserRole.Student, new Actor("admin-1", UserRole.Admin)));

        Assert.Equal(isValid, result.IsValid);
    }
}
