using ClosedXML.Excel;
using Exam.Application.CategoryAggregate.Commands.CreateCategory;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Application.QuestionAggregate.Commands.DeleteQuestion;
using Exam.Application.QuestionAggregate.Commands.ImportQuestions;
using Exam.Application.QuestionAggregate.Commands.MoveQuestions;
using Exam.Application.QuestionAggregate.Commands.UpdateQuestion;
using Exam.Application.QuestionAggregate.Queries.ExportQuestions;
using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;
using Exam.Contracts;
using FluentValidation;

namespace Exam.Infrastructure.IntegrationTest.QuestionAggregate;

public class QuestionAggregateIntegrationTests
{
    private readonly IntegrationTestFixture _fixture = new();

    private static readonly IReadOnlyCollection<AnswerInput> ValidAnswers =
    [
        new AnswerInput("Đáp án A", true),
        new AnswerInput("Đáp án B", false)
    ];

    private async Task<CategoryDto> CreateCategoryAsync(string name = "Toán học", string urlPath = "toan-hoc") =>
        await _fixture.Mediator.Send(new CreateCategoryCommand(name, urlPath));

    [Fact]
    public async Task CreateQuestion_ValidCommand_InsertsQuestionAndWritesAuditLog()
    {
        var category = await CreateCategoryAsync();
        _fixture.CurrentUser.UserId = "teacher-1";

        var result = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "", "teacher-1"));

        Assert.Equal(category.Name, result.CategoryName);
        Assert.Single(_fixture.QuestionRepository.Items);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Question.Create");
    }

    [Fact]
    public async Task CreateQuestion_InvalidCommand_ThrowsValidationExceptionAndInsertsNothing()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _fixture.Mediator.Send(new CreateQuestionCommand("", QuestionType.SingleSelection, Level.Easy,
                category.Id, ValidAnswers, "", "teacher-1")));

        Assert.Empty(_fixture.QuestionRepository.Items);
    }

    [Fact]
    public async Task CreateQuestion_CategoryNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
                Level.Easy, "cat-không-tồn-tại", ValidAnswers, "", "teacher-1")));
    }

    [Fact]
    public async Task UpdateQuestion_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var category = await CreateCategoryAsync();
        var question = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "", "teacher-1"));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _fixture.Mediator.Send(new UpdateQuestionCommand(question.Id, "Nội dung mới", QuestionType.SingleSelection,
                Level.Easy, category.Id, ValidAnswers, "", new Actor("other-teacher", UserRole.Instructor))));
    }

    [Fact]
    public async Task UpdateQuestion_Owner_UpdatesQuestionAndWritesAuditLog()
    {
        var category = await CreateCategoryAsync();
        var question = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "", "teacher-1"));
        _fixture.CurrentUser.UserId = "teacher-1";

        var updated = await _fixture.Mediator.Send(new UpdateQuestionCommand(question.Id, "Nội dung mới", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "Giải thích mới", new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal("Nội dung mới", updated.Content);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Question.Update");
    }

    [Fact]
    public async Task DeleteQuestion_Owner_DeletesQuestionAndWritesAuditLog()
    {
        var category = await CreateCategoryAsync();
        var question = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "", "teacher-1"));
        _fixture.CurrentUser.UserId = "teacher-1";

        await _fixture.Mediator.Send(new DeleteQuestionCommand(question.Id, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Empty(_fixture.QuestionRepository.Items);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Question.Delete");
    }

    [Fact]
    public async Task DeleteQuestion_NotOwnerOrAdmin_ThrowsForbiddenExceptionAndDoesNotDelete()
    {
        var category = await CreateCategoryAsync();
        var question = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "", "teacher-1"));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _fixture.Mediator.Send(new DeleteQuestionCommand(question.Id, new Actor("other-teacher", UserRole.Instructor))));

        Assert.Single(_fixture.QuestionRepository.Items);
    }

    [Fact]
    public async Task MoveQuestions_ValidRequest_MovesQuestionsToTargetCategoryAndWritesAuditLog()
    {
        var source = await CreateCategoryAsync("Toán học", "toan-hoc");
        var target = await CreateCategoryAsync("Vật lý", "vat-ly");
        var question = await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, source.Id, ValidAnswers, "", "teacher-1"));
        _fixture.CurrentUser.UserId = "teacher-1";

        await _fixture.Mediator.Send(new MoveQuestionsCommand([question.Id], target.Id, new Actor("teacher-1", UserRole.Instructor)));

        var moved = _fixture.QuestionRepository.Items.Single();
        Assert.Equal(target.Id, moved.CategoryId);
        Assert.Equal(target.Name, moved.CategoryName);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Question.MoveQuestions");
    }

    private static byte[] BuildImportExcel(params (string CategoryName, string Content, string Correct)[] rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        var headers = new[] { "Môn học", "Mức độ", "Loại câu hỏi", "Nội dung", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D",
            "Đáp án đúng", "Giải thích" };
        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        for (var i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value = r.CategoryName;
            worksheet.Cell(row, 2).Value = "Dễ";
            worksheet.Cell(row, 3).Value = "Một đáp án";
            worksheet.Cell(row, 4).Value = r.Content;
            worksheet.Cell(row, 5).Value = "Đáp án A";
            worksheet.Cell(row, 6).Value = "Đáp án B";
            worksheet.Cell(row, 9).Value = r.Correct;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    [Fact]
    public async Task ImportQuestions_NotDryRun_InsertsQuestionsAndWritesManualAuditLog()
    {
        var category = await CreateCategoryAsync();
        var excel = BuildImportExcel((category.Name, "Nội dung nhập từ Excel", "A"));

        var result = await _fixture.Mediator.Send(new ImportQuestionsCommand(excel, "teacher-1", DryRun: false));

        Assert.Equal(1, result.SuccessCount);
        Assert.Single(_fixture.QuestionRepository.Items);
        // ImportQuestionsCommand tự khai ISkipAutoAuditLog nên AuditLoggingBehavior bỏ qua - log này
        // phải do chính handler tự ghi thủ công (xem comment trong ImportQuestionsCommandHandler).
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Question.Import");
    }

    [Fact]
    public async Task ImportQuestions_DryRun_DoesNotInsertOrWriteAuditLog()
    {
        var category = await CreateCategoryAsync();
        var excel = BuildImportExcel((category.Name, "Nội dung nhập từ Excel", "A"));

        var result = await _fixture.Mediator.Send(new ImportQuestionsCommand(excel, "teacher-1", DryRun: true));

        Assert.Equal(1, result.SuccessCount);
        Assert.Empty(_fixture.QuestionRepository.Items);
        Assert.Empty(_fixture.AuditLogRepository.Items);
    }

    [Fact]
    public async Task GetQuestionsByCategoryPaged_ReturnsPagedResultMatchingInsertedQuestions()
    {
        var category = await CreateCategoryAsync();
        await _fixture.Mediator.Send(new CreateQuestionCommand("Câu 1", QuestionType.SingleSelection, Level.Easy,
            category.Id, ValidAnswers, "", "teacher-1"));
        await _fixture.Mediator.Send(new CreateQuestionCommand("Câu 2", QuestionType.SingleSelection, Level.Easy,
            category.Id, ValidAnswers, "", "teacher-1"));

        var result = await _fixture.Mediator.Send(new GetQuestionsByCategoryPagedQuery(category.Id, 1, 20));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2L, result.TotalCount);
    }

    [Fact]
    public async Task ExportQuestions_ProducesExcelWithHeaderAndQuestionRow()
    {
        var category = await CreateCategoryAsync();
        await _fixture.Mediator.Send(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "Giải thích", "teacher-1"));

        var bytes = await _fixture.Mediator.Send(new ExportQuestionsQuery(category.Id));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Môn học", worksheet.Cell(1, 1).GetString());
        Assert.Equal(category.Name, worksheet.Cell(2, 1).GetString());
        Assert.Equal("Nội dung câu hỏi", worksheet.Cell(2, 4).GetString());
    }
}
