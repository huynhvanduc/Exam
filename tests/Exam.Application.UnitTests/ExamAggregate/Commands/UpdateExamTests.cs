using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.UpdateExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class UpdateExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề cũ", "", "Nội dung cũ",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new UpdateExamCommandHandler(examRepository.Object, new Mock<ICategoryRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateExamCommand("exam-1", "Đề mới", "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
                "cat-1", true, 5m, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UpdateExamCommandHandler(examRepository.Object, new Mock<ICategoryRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdateExamCommand(exam.Id, "Đề mới", "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
                "cat-1", true, 5m, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-2", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new UpdateExamCommandHandler(examRepository.Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateExamCommand(exam.Id, "Đề mới", "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
                "cat-2", true, 5m, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesExamAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var newCategory = Category.Create("Vật lý", "vat-ly").WithId("cat-2");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(newCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newCategory);
        var handler = new UpdateExamCommandHandler(examRepository.Object, categoryRepository.Object);

        var result = await handler.Handle(new UpdateExamCommand(exam.Id, "Đề mới", "Mô tả mới", "Nội dung mới",
            TimeSpan.FromMinutes(90), Level.Difficult, newCategory.Id, false, 7m, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal("Đề mới", result.Name);
        Assert.Equal(newCategory.Id, result.CategoryId);
        Assert.Equal(newCategory.Name, result.CategoryName);
        Assert.Equal(7m, result.MinimumPassingScore);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExamNotDraft_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var handler = new UpdateExamCommandHandler(examRepository.Object, categoryRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new UpdateExamCommand(exam.Id, "Đề mới", "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
                category.Id, true, 5m, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }
}

public class UpdateExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UpdateExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, string name, string categoryId, bool isValid)
    {
        var validator = new UpdateExamCommandValidator();

        var result = validator.Validate(new UpdateExamCommand(id, name, "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
            categoryId, true, 5m, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
