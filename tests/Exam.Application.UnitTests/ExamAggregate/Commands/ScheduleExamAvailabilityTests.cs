using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.ScheduleExamAvailability;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class ScheduleExamAvailabilityCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new ScheduleExamAvailabilityCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ScheduleExamAvailabilityCommand("exam-1", null, null, new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ScheduleExamAvailabilityCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ScheduleExamAvailabilityCommand(exam.Id, null, null, new Actor("other-teacher", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ArchivedExam_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.Archive();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ScheduleExamAvailabilityCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new ScheduleExamAvailabilityCommand(exam.Id, DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
                new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AvailableToBeforeAvailableFrom_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ScheduleExamAvailabilityCommandHandler(examRepository.Object);
        var from = DateTime.UtcNow.AddDays(1);
        var to = DateTime.UtcNow;

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new ScheduleExamAvailabilityCommand(exam.Id, from, to, new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_SchedulesAvailabilityAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ScheduleExamAvailabilityCommandHandler(examRepository.Object);
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow.AddDays(7);

        var result = await handler.Handle(new ScheduleExamAvailabilityCommand(exam.Id, from, to, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal(from, result.AvailableFrom);
        Assert.Equal(to, result.AvailableTo);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ScheduleExamAvailabilityCommandValidatorTests
{
    [Fact]
    public void Validate_EmptyExamId_IsInvalid()
    {
        var validator = new ScheduleExamAvailabilityCommandValidator();

        var result = validator.Validate(new ScheduleExamAvailabilityCommand("", null, null, new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NeitherDateSet_IsValid()
    {
        var validator = new ScheduleExamAvailabilityCommandValidator();

        var result = validator.Validate(new ScheduleExamAvailabilityCommand("exam-1", null, null, new Actor("teacher-1", UserRole.Instructor)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_OnlyAvailableFromSet_IsValid()
    {
        var validator = new ScheduleExamAvailabilityCommandValidator();

        var result = validator.Validate(new ScheduleExamAvailabilityCommand("exam-1", DateTime.UtcNow, null,
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AvailableToAfterAvailableFrom_IsValid()
    {
        var validator = new ScheduleExamAvailabilityCommandValidator();

        var result = validator.Validate(new ScheduleExamAvailabilityCommand("exam-1", DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AvailableToBeforeAvailableFrom_IsInvalid()
    {
        var validator = new ScheduleExamAvailabilityCommandValidator();

        var result = validator.Validate(new ScheduleExamAvailabilityCommand("exam-1", DateTime.UtcNow, DateTime.UtcNow.AddDays(-1),
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }
}
