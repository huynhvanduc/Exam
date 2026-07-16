using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class ConfigureExamCompositionCommandHandlerTests
{
    private static readonly IReadOnlyCollection<ExamCompositionCellDto> ValidCells =
        [new ExamCompositionCellDto(Level.Easy, QuestionType.SingleSelection, 5)];

    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new ConfigureExamCompositionCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ConfigureExamCompositionCommand("exam-1", ValidCells, new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureExamCompositionCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ConfigureExamCompositionCommand(exam.Id, ValidCells, new Actor("other-teacher", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExamNotDraft_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureExamCompositionCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new ConfigureExamCompositionCommand(exam.Id, ValidCells, new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ConfiguresCompositionAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ConfigureExamCompositionCommandHandler(examRepository.Object);
        IReadOnlyCollection<ExamCompositionCellDto> cells =
        [
            new ExamCompositionCellDto(Level.Easy, QuestionType.SingleSelection, 3),
            new ExamCompositionCellDto(Level.Difficult, QuestionType.MultipleSelection, 2)
        ];

        var result = await handler.Handle(new ConfigureExamCompositionCommand(exam.Id, cells, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal(5, result.NumberOfQuestions);
        Assert.Equal(2, result.Composition.Count);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ConfigureExamCompositionCommandValidatorTests
{
    private static readonly IReadOnlyCollection<ExamCompositionCellDto> ValidCells =
        [new ExamCompositionCellDto(Level.Easy, QuestionType.SingleSelection, 5)];

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var validator = new ConfigureExamCompositionCommandValidator();

        var result = validator.Validate(new ConfigureExamCompositionCommand("exam-1", ValidCells,
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyExamId_IsInvalid()
    {
        var validator = new ConfigureExamCompositionCommandValidator();

        var result = validator.Validate(new ConfigureExamCompositionCommand("", ValidCells,
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullCells_IsInvalid()
    {
        var validator = new ConfigureExamCompositionCommandValidator();

        var result = validator.Validate(new ConfigureExamCompositionCommand("exam-1", null!,
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeCellCount_IsInvalid()
    {
        var validator = new ConfigureExamCompositionCommandValidator();

        var result = validator.Validate(new ConfigureExamCompositionCommand("exam-1",
            [new ExamCompositionCellDto(Level.Easy, QuestionType.SingleSelection, -1)], new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TotalCountZero_IsInvalid()
    {
        var validator = new ConfigureExamCompositionCommandValidator();

        var result = validator.Validate(new ConfigureExamCompositionCommand("exam-1",
            [new ExamCompositionCellDto(Level.Easy, QuestionType.SingleSelection, 0)], new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }
}
