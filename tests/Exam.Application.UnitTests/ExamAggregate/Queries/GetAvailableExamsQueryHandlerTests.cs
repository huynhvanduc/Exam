using Exam.Application.ExamAggregate.Queries.GetAvailableExams;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Queries;

public class GetAvailableExamsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResultUsingCallerClassMemberships()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByMemberAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync([classRoom]);
        var exams = new[]
        {
            new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
                "teacher-1", "cat-1", "Toán học", true, 5m)
        };
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetAvailableForUserAsync(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), 0, 20,
            It.IsAny<CancellationToken>())).ReturnsAsync(exams);
        examRepository.Setup(r => r.CountAvailableForUserAsync(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetAvailableExamsQueryHandler(examRepository.Object, classRoomRepository.Object);

        var result = await handler.Handle(new GetAvailableExamsQuery("student-1", 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);
    }

    [Fact]
    public async Task Handle_NoClassMemberships_StillReturnsPublicExams()
    {
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByMemberAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var exams = new[]
        {
            new Domain.AggregateModels.ExamAggregate.Exam("Đề công khai", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
                "teacher-1", "cat-1", "Toán học", true, 5m)
        };
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetAvailableForUserAsync(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), 0, 20,
            It.IsAny<CancellationToken>())).ReturnsAsync(exams);
        examRepository.Setup(r => r.CountAvailableForUserAsync(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetAvailableExamsQueryHandler(examRepository.Object, classRoomRepository.Object);

        var result = await handler.Handle(new GetAvailableExamsQuery("student-1", 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
    }
}

public class GetAvailableExamsQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetAvailableExamsQueryValidator.csv",
        CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(int page, int pageSize, bool isValid)
    {
        var validator = new GetAvailableExamsQueryValidator();

        var result = validator.Validate(new GetAvailableExamsQuery("student-1", page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
