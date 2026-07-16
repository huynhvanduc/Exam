using Exam.Application.UserAggregate.Queries.GetUsers;
using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.UserAggregate.Queries;

public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForwardsFiltersToRepository_AndMapsToDto()
    {
        var users = new[] { new User("ext-1", "user@example.com", "Nguyễn", "Văn A") };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetPagedAsync(0, 20, "nguyễn", UserRole.Student, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        repository.Setup(r => r.CountAsync("nguyễn", UserRole.Student, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        var handler = new GetUsersQueryHandler(repository.Object);

        var result = await handler.Handle(new GetUsersQuery(1, 20, "nguyễn", UserRole.Student, true), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("ext-1", result.Items.First().ExternalId);
        Assert.Equal(1L, result.TotalCount);
    }
}

public class GetUsersQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetUsersQueryValidator.csv",
        CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(int page, int pageSize, bool isValid)
    {
        var validator = new GetUsersQueryValidator();

        var result = validator.Validate(new GetUsersQuery(page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
