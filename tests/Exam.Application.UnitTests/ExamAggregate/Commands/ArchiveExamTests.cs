using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.ArchiveExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class ArchiveExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new ArchiveExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ArchiveExamCommand("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ArchiveExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ArchiveExamCommand(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyArchived_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.Archive();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ArchiveExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new ArchiveExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ArchivesExamAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ArchiveExamCommandHandler(examRepository.Object);

        var result = await handler.Handle(new ArchiveExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(ExamStatus.Archived, result.Status);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ArchiveExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("ArchiveExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, bool isValid)
    {
        var validator = new ArchiveExamCommandValidator();

        var result = validator.Validate(new ArchiveExamCommand(examId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
