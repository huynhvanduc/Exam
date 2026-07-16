using ClosedXML.Excel;
using Exam.Application.QuestionAggregate.Queries.ExportQuestions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Queries;

public class ExportQuestionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_ProducesExcelWithHeaderAndQuestionRow()
    {
        var question = new Question(null!, "Nội dung câu hỏi", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer(null!, "Đáp án A", true), new Answer(null!, "Đáp án B", false)], "Giải thích", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        var handler = new ExportQuestionsQueryHandler(questionRepository.Object);

        var bytes = await handler.Handle(new ExportQuestionsQuery("cat-1"), CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Môn học", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Toán học", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Dễ", worksheet.Cell(2, 2).GetString());
        Assert.Equal("Một đáp án", worksheet.Cell(2, 3).GetString());
        Assert.Equal("Nội dung câu hỏi", worksheet.Cell(2, 4).GetString());
        Assert.Equal("Đáp án A", worksheet.Cell(2, 5).GetString());
        Assert.Equal("A", worksheet.Cell(2, 9).GetString());
        Assert.Equal("Giải thích", worksheet.Cell(2, 10).GetString());
    }

    [Fact]
    public async Task Handle_MultipleCorrectAnswers_ListsAllCorrectLettersCommaSeparated()
    {
        var question = new Question(null!, "Nội dung", QuestionType.MultipleSelection, Level.Medium, "cat-1",
            [new Answer(null!, "Đáp án A", true), new Answer(null!, "Đáp án B", true), new Answer(null!, "Đáp án C", false)],
            "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        var handler = new ExportQuestionsQueryHandler(questionRepository.Object);

        var bytes = await handler.Handle(new ExportQuestionsQuery("cat-1"), CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("A,B", worksheet.Cell(2, 9).GetString());
    }

    [Fact]
    public async Task Handle_NoQuestions_ProducesHeaderOnlyWorkbook()
    {
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new ExportQuestionsQueryHandler(questionRepository.Object);

        var bytes = await handler.Handle(new ExportQuestionsQuery("cat-1"), CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Môn học", worksheet.Cell(1, 1).GetString());
        Assert.True(string.IsNullOrEmpty(worksheet.Cell(2, 1).GetString()));
    }
}
