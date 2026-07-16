using Exam.Application.ExamResultAggregate.Queries.GetMyExamHistory;
using Exam.Domain.AggregateModels.ExamResultAggregate;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetMyExamHistoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResultUsingSkipTakeAndCount()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByUserIdAsync("student-1", 20, 20, It.IsAny<CancellationToken>())).ReturnsAsync([examResult]);
        examResultRepository.Setup(r => r.CountByUserIdAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetMyExamHistoryQueryHandler(examResultRepository.Object);

        var result = await handler.Handle(new GetMyExamHistoryQuery("student-1", 2, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);
    }
}

public class GetMyExamHistoryQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetMyExamHistoryQueryValidator.csv",
        CsvTestData.Str, CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string userId, int page, int pageSize, bool isValid)
    {
        var validator = new GetMyExamHistoryQueryValidator();

        var result = validator.Validate(new GetMyExamHistoryQuery(userId, page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
