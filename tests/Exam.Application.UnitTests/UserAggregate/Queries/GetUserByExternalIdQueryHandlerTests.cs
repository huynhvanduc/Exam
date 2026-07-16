using Exam.Application.UserAggregate.Queries.GetUserByExternalId;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.UserAggregate.Queries;

public class GetUserByExternalIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUser_ReturnsDto()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("ext-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new GetUserByExternalIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUserByExternalIdQuery("ext-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ext-1", result!.ExternalId);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ReturnsNull()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByExternalIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((User)null!);
        var handler = new GetUserByExternalIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUserByExternalIdQuery("missing"), CancellationToken.None);

        Assert.Null(result);
    }
}
