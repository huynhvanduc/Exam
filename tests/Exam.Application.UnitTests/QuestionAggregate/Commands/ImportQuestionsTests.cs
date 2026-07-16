using ClosedXML.Excel;
using Exam.Application.QuestionAggregate.Commands.ImportQuestions;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Commands;

public class ImportQuestionsCommandHandlerTests
{
    private static readonly string[] Headers =
    [
        "Môn học", "Mức độ", "Loại câu hỏi", "Nội dung", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D",
        "Đáp án đúng", "Giải thích"
    ];

    private static string?[] ValidRow(string? categoryName = "Toán học", string? level = "Dễ", string? questionType = "Một đáp án",
        string? content = "Nội dung câu hỏi", string? a = "Đáp án A", string? b = "Đáp án B", string? c = "", string? d = "",
        string? correct = "A", string? explain = "") =>
        [categoryName, level, questionType, content, a, b, c, d, correct, explain];

    // Dựng file Excel tối giản trong bộ nhớ theo đúng layout cột của handler (xem ImportQuestionsCommandHandler)
    // - tránh phải kèm file mẫu nhị phân trong repo.
    private static byte[] BuildExcel(params string?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        for (var i = 0; i < Headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];

        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                worksheet.Cell(r + 2, c + 1).Value = rows[r][c] ?? "";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static (Mock<IQuestionRepository> QuestionRepository, Mock<ICategoryRepository> CategoryRepository,
        Mock<IAuditLogRepository> AuditLogRepository, ImportQuestionsCommandHandler Handler) CreateSut(params Category[] categories)
    {
        var questionRepository = new Mock<IQuestionRepository>();
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new ImportQuestionsCommandHandler(questionRepository.Object, categoryRepository.Object, auditLogRepository.Object);

        return (questionRepository, categoryRepository, auditLogRepository, handler);
    }

    [Fact]
    public async Task Handle_ValidRow_InsertsQuestionAndReturnsSuccessCount()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (questionRepository, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow()), "teacher-1", false), CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Empty(result.Errors);
        questionRepository.Verify(r => r.InsertAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_RecordsRowError()
    {
        var (_, _, _, handler) = CreateSut();

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(categoryName: "Không tồn tại")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Không tìm thấy môn học", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_InvalidLevel_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(level: "Siêu khó")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Mức độ không hợp lệ", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_InvalidQuestionType_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(questionType: "Không xác định")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Loại câu hỏi không hợp lệ", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_MissingContent_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(content: "")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Thiếu nội dung câu hỏi", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_MissingAnswerB_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(b: "")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Đáp án A và B là bắt buộc", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_MissingCorrectLetters_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(correct: "")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("Thiếu đáp án đúng", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_CorrectLetterReferencesNonExistentAnswer_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(correct: "C")),
            "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("tham chiếu đáp án không tồn tại", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_SingleSelectionWithMultipleCorrectLetters_RecordsRowError()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(
            BuildExcel(ValidRow(questionType: "Một đáp án", correct: "A,B")), "teacher-1", false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("chỉ được có đúng 1 đáp án đúng", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_MultipleSelectionWithMultipleCorrectLetters_Succeeds()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (questionRepository, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(
            BuildExcel(ValidRow(questionType: "Nhiều đáp án", correct: "A,B")), "teacher-1", false), CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        Assert.Empty(result.Errors);
        questionRepository.Verify(r => r.InsertAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BlankRow_SkippedAndNotCounted()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (_, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(
            BuildExcel(ValidRow(), ["", "", "", "", "", "", "", "", "", ""]), "teacher-1", false), CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
    }

    [Fact]
    public async Task Handle_DryRun_DoesNotInsertOrWriteAuditLog()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (questionRepository, _, auditLogRepository, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow()), "teacher-1", true), CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        questionRepository.Verify(r => r.InsertAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Never);
        auditLogRepository.Verify(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotDryRun_InsertsAndWritesAuditLogWithCategorySummary()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (questionRepository, _, auditLogRepository, handler) = CreateSut(category);
        AuditLogEntry? capturedEntry = null;
        auditLogRepository.Setup(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLogEntry, CancellationToken>((entry, _) => capturedEntry = entry)
            .Returns(Task.CompletedTask);

        await handler.Handle(new ImportQuestionsCommand(BuildExcel(ValidRow(), ValidRow()), "teacher-1", false), CancellationToken.None);

        questionRepository.Verify(r => r.InsertAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.NotNull(capturedEntry);
        Assert.Equal("Question.Import", capturedEntry!.Action);
        Assert.Contains("Toán học (2)", capturedEntry.Description);
    }

    [Fact]
    public async Task Handle_MixedValidAndInvalidRows_ReturnsCorrectTotals()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var (questionRepository, _, _, handler) = CreateSut(category);

        var result = await handler.Handle(new ImportQuestionsCommand(
            BuildExcel(ValidRow(), ValidRow(content: "")), "teacher-1", false), CancellationToken.None);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Single(result.Errors);
        questionRepository.Verify(r => r.InsertAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ImportQuestionsCommandValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var validator = new ImportQuestionsCommandValidator();

        var result = validator.Validate(new ImportQuestionsCommand([1, 2, 3], "teacher-1", false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyFileContent_IsInvalid()
    {
        var validator = new ImportQuestionsCommandValidator();

        var result = validator.Validate(new ImportQuestionsCommand([], "teacher-1", false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyOwnerUserId_IsInvalid()
    {
        var validator = new ImportQuestionsCommandValidator();

        var result = validator.Validate(new ImportQuestionsCommand([1, 2, 3], "", false));

        Assert.False(result.IsValid);
    }
}
