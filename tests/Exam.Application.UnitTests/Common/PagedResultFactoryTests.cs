using Exam.Contracts;

namespace Exam.Application.UnitTests.Common;

public class PagedResultFactoryTests
{
    public static IEnumerable<object?[]> CreateAsyncCases() => CsvTestData.Read("PagedResultFactory_CreateAsync.csv",
        CsvTestData.Int, CsvTestData.Int, CsvTestData.Int);

    [Theory]
    [MemberData(nameof(CreateAsyncCases))]
    public async Task CreateAsync_ComputesCorrectSkip(int page, int pageSize, int expectedSkip)
    {
        int? capturedSkip = null;
        int? capturedTake = null;

        var result = await PagedResultFactory.CreateAsync<string>(page, pageSize,
            (skip, take) =>
            {
                capturedSkip = skip;
                capturedTake = take;
                return Task.FromResult<IReadOnlyCollection<string>>(["item"]);
            },
            () => Task.FromResult(42L));

        Assert.Equal(expectedSkip, capturedSkip);
        Assert.Equal(pageSize, capturedTake);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(42L, result.TotalCount);
        Assert.Single(result.Items);
    }
}
