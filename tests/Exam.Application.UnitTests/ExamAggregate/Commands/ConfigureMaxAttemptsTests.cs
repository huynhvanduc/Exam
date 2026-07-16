using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class ConfigureMaxAttemptsCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new ConfigureMaxAttemptsCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ConfigureMaxAttemptsCommand("exam-1", 3, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureMaxAttemptsCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ConfigureMaxAttemptsCommand(exam.Id, 3, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ZeroMaxAttempts_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureMaxAttemptsCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new ConfigureMaxAttemptsCommand(exam.Id, 0, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_SetsMaxAttemptsAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureMaxAttemptsCommandHandler(examRepository.Object);

        var result = await handler.Handle(new ConfigureMaxAttemptsCommand(exam.Id, 3, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal(3, result.MaxAttempts);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullMaxAttempts_ClearsLimit()
    {
        var exam = CreateExam("teacher-1");
        exam.ConfigureMaxAttempts(3);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureMaxAttemptsCommandHandler(examRepository.Object);

        var result = await handler.Handle(new ConfigureMaxAttemptsCommand(exam.Id, null, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Null(result.MaxAttempts);
    }
}

public class ConfigureMaxAttemptsCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("ConfigureMaxAttemptsCommandValidator.csv",
        CsvTestData.Str, CsvTestData.NullableInt, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, int? maxAttempts, bool isValid)
    {
        var validator = new ConfigureMaxAttemptsCommandValidator();

        var result = validator.Validate(new ConfigureMaxAttemptsCommand(examId, maxAttempts, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
