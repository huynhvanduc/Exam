using Exam.Application.ExamAggregate.Queries.GetExamsByCategory;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Queries;

public class GetExamsByCategoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResultUsingSkipTakeAndCount()
    {
        var exams = new[]
        {
            new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
                "teacher-1", "cat-1", "Toán học", true, 5m)
        };
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByCategoryAsync("cat-1", 20, 20, It.IsAny<CancellationToken>())).ReturnsAsync(exams);
        examRepository.Setup(r => r.CountByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetExamsByCategoryQueryHandler(examRepository.Object);

        var result = await handler.Handle(new GetExamsByCategoryQuery("cat-1", 2, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);
    }
}

public class GetExamsByCategoryQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetExamsByCategoryQueryValidator.csv",
        CsvTestData.Str, CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string categoryId, int page, int pageSize, bool isValid)
    {
        var validator = new GetExamsByCategoryQueryValidator();

        var result = validator.Validate(new GetExamsByCategoryQuery(categoryId, page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
