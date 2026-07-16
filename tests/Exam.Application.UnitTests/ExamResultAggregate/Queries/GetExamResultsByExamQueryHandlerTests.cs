using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.GetExamResultsByExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamResultsByExamQueryHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) =>
        new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
            ownerUserId, "cat-1", "Toán học", true, 5m).WithId("exam-1");

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new GetExamResultsByExamQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetExamResultsByExamQuery("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new GetExamResultsByExamQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetExamResultsByExamQuery(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPagedResult()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResult = new ExamResult("student-1", exam.Id).WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 20, 20, It.IsAny<CancellationToken>())).ReturnsAsync([examResult]);
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetExamResultsByExamQueryHandler(examRepository.Object, examResultRepository.Object);

        var result = await handler.Handle(new GetExamResultsByExamQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor), 2, 20),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);
    }
}

public class GetExamResultsByExamQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetExamResultsByExamQueryValidator.csv",
        CsvTestData.Str, CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, int page, int pageSize, bool isValid)
    {
        var validator = new GetExamResultsByExamQueryValidator();

        var result = validator.Validate(new GetExamResultsByExamQuery(examId, new Actor("teacher-1", UserRole.Instructor), page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
