using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Commands.RecordAnswer;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Commands;

public class RecordAnswerCommandHandlerTests
{
    private static ExamResult CreateInProgressResult(string userId, TimeSpan? duration = null)
    {
        var examResult = new ExamResult(userId, "exam-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.SetDuration(duration);
        return examResult;
    }

    [Fact]
    public async Task Handle_ExamResultNotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new RecordAnswerCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RecordAnswerCommand("result-1", "student-1", "q-1", ["a-1"]), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var examResult = CreateInProgressResult("student-1");
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new RecordAnswerCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new RecordAnswerCommand(examResult.Id, "other-student", "q-1", ["a-1"]), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Expired_AutoFinishesAndReturnsFinishedTrue()
    {
        var examResult = CreateInProgressResult("student-1", TimeSpan.FromMinutes(-1));
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var gradingService = new ExamResultGradingService(examRepository.Object, questionRepository.Object);
        var handler = new RecordAnswerCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new RecordAnswerCommand(examResult.Id, "student-1", "q-1", ["a-1"]), CancellationToken.None);

        Assert.True(result.Finished);
        Assert.True(examResult.Finished);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_RecordsAnswerAndReturnsFinishedFalse()
    {
        var examResult = CreateInProgressResult("student-1");
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new RecordAnswerCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new RecordAnswerCommand(examResult.Id, "student-1", "q-1", ["a-1"]), CancellationToken.None);

        Assert.False(result.Finished);
        Assert.False(examResult.Finished);
        Assert.Single(examResult.DraftAnswers);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RecordAnswerCommandValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var validator = new RecordAnswerCommandValidator();

        var result = validator.Validate(new RecordAnswerCommand("result-1", "student-1", "q-1", ["a-1"]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyExamResultId_IsInvalid()
    {
        var validator = new RecordAnswerCommandValidator();

        var result = validator.Validate(new RecordAnswerCommand("", "student-1", "q-1", ["a-1"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyUserId_IsInvalid()
    {
        var validator = new RecordAnswerCommandValidator();

        var result = validator.Validate(new RecordAnswerCommand("result-1", "", "q-1", ["a-1"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyQuestionId_IsInvalid()
    {
        var validator = new RecordAnswerCommandValidator();

        var result = validator.Validate(new RecordAnswerCommand("result-1", "student-1", "", ["a-1"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullSelectedAnswerIds_IsInvalid()
    {
        var validator = new RecordAnswerCommandValidator();

        var result = validator.Validate(new RecordAnswerCommand("result-1", "student-1", "q-1", null!));

        Assert.False(result.IsValid);
    }
}
