using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Commands.StartExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Commands;

public class StartExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreatePublishedExam(int cellCount = 1, int? maxAttempts = null)
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, "teacher-1", "cat-1", "Toán học", false, 5m).WithId("exam-1");
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, cellCount)]);
        if (maxAttempts.HasValue)
            exam.ConfigureMaxAttempts(maxAttempts.Value);
        exam.Publish();
        return exam;
    }

    private static StartExamCommandHandler CreateHandler(
        Mock<IExamRepository> examRepository, Mock<IExamResultRepository> examResultRepository,
        Mock<IQuestionRepository> questionRepository, Mock<IUserRepository> userRepository,
        Mock<IClassRoomRepository> classRoomRepository) =>
        new(examRepository.Object, examResultRepository.Object, questionRepository.Object, userRepository.Object,
            classRoomRepository.Object, new ExamQuestionPoolService(questionRepository.Object));

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = CreateHandler(examRepository, new Mock<IExamResultRepository>(), new Mock<IQuestionRepository>(),
            new Mock<IUserRepository>(), new Mock<IClassRoomRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new StartExamCommand("exam-1", "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingInProgressAttempt_ReturnsExistingAttemptWithoutInsertingNew()
    {
        var exam = CreatePublishedExam();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var existingResult = new ExamResult("student-1", exam.Id).WithId("result-1");
        existingResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingResult);
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(existingResult.QuestionIds, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = CreateHandler(examRepository, examResultRepository, questionRepository, new Mock<IUserRepository>(),
            new Mock<IClassRoomRepository>());

        var result = await handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None);

        Assert.Equal(existingResult.Id, result.Id);
        examResultRepository.Verify(r => r.InsertAsync(It.IsAny<ExamResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExamNotAvailable_ThrowsDomainException()
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, "teacher-1", "cat-1", "Toán học", false, 5m).WithId("exam-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamResult)null!);
        var handler = CreateHandler(examRepository, examResultRepository, new Mock<IQuestionRepository>(),
            new Mock<IUserRepository>(), new Mock<IClassRoomRepository>());

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MaxAttemptsReached_ThrowsDomainException()
    {
        var exam = CreatePublishedExam(maxAttempts: 1);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamResult)null!);
        examResultRepository.Setup(r => r.CountByUserIdAndExamIdAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var handler = CreateHandler(examRepository, examResultRepository, new Mock<IQuestionRepository>(),
            new Mock<IUserRepository>(), new Mock<IClassRoomRepository>());

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotAssignedToClass_ThrowsForbiddenException()
    {
        var exam = CreatePublishedExam();
        exam.AssignToClass("class-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamResult)null!);
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByMemberAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = CreateHandler(examRepository, examResultRepository, new Mock<IQuestionRepository>(),
            new Mock<IUserRepository>(), classRoomRepository);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_DrawsQuestionsAndCreatesNewAttempt()
    {
        var exam = CreatePublishedExam();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamResult)null!);
        var question = new Question("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a-1", "Đáp án A", true)], "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryLevelTypeAsync("cat-1", Level.Easy, QuestionType.SingleSelection,
            It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdAsync("student-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("student-1", "student1@example.com", "Nguyễn", "Văn A"));
        // Mô phỏng MongoDB driver gán Id khi Insert thật - Moq mặc định không tự mutate đối tượng.
        examResultRepository.Setup(r => r.InsertAsync(It.IsAny<ExamResult>(), It.IsAny<CancellationToken>()))
            .Callback<ExamResult, CancellationToken>((er, _) => er.WithId("result-1"))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(examRepository, examResultRepository, questionRepository, userRepository,
            new Mock<IClassRoomRepository>());

        var result = await handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None);

        Assert.Equal(exam.Id, result.ExamId);
        Assert.Single(result.Questions);
        examResultRepository.Verify(r => r.InsertAsync(
            It.Is<ExamResult>(er => er.UserId == "student-1" && er.QuestionIds.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotEnoughQuestionsInBank_ThrowsDomainException()
    {
        var exam = CreatePublishedExam(cellCount: 5);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetInProgressAttemptAsync("student-1", exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExamResult)null!);
        var question = new Question(null!, "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer(null!, "Đáp án A", true)], "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryLevelTypeAsync("cat-1", Level.Easy, QuestionType.SingleSelection,
            It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        var handler = CreateHandler(examRepository, examResultRepository, questionRepository, new Mock<IUserRepository>(),
            new Mock<IClassRoomRepository>());

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new StartExamCommand(exam.Id, "student-1"), CancellationToken.None));

        examResultRepository.Verify(r => r.InsertAsync(It.IsAny<ExamResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class StartExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("StartExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, string userId, bool isValid)
    {
        var validator = new StartExamCommandValidator();

        var result = validator.Validate(new StartExamCommand(examId, userId));

        Assert.Equal(isValid, result.IsValid);
    }
}
