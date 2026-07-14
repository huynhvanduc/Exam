using ClosedXML.Excel;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.ExportQuestions;

public class ExportQuestionsQueryHandler : IRequestHandler<ExportQuestionsQuery, byte[]>
{
    private static readonly string[] Headers =
    [
        "Môn học", "Mức độ", "Loại câu hỏi", "Nội dung câu hỏi",
        "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích"
    ];

    private readonly IQuestionRepository _questionRepository;

    public ExportQuestionsQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<byte[]> Handle(ExportQuestionsQuery request, CancellationToken cancellationToken)
    {
        var questions = await _questionRepository.GetByCategoryAsync(request.CategoryId, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Questions");

        for (var i = 0; i < Headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var question in questions)
        {
            var answers = question.Answers.ToList();

            worksheet.Cell(rowNumber, 1).Value = question.CategoryName;
            worksheet.Cell(rowNumber, 2).Value = LevelLabel(question.Level);
            worksheet.Cell(rowNumber, 3).Value = QuestionTypeLabel(question.QuestionType);
            worksheet.Cell(rowNumber, 4).Value = question.Content;

            for (var i = 0; i < answers.Count && i < 4; i++)
                worksheet.Cell(rowNumber, 5 + i).Value = answers[i].Content;

            var correctLetters = answers
                .Take(4)
                .Select((answer, index) => (Answer: answer, Letter: (char)('A' + index)))
                .Where(x => x.Answer.IsCorrect)
                .Select(x => x.Letter.ToString());
            worksheet.Cell(rowNumber, 9).Value = string.Join(",", correctLetters);

            worksheet.Cell(rowNumber, 10).Value = question.Explain;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string LevelLabel(Level level) => level switch
    {
        Level.Easy => "Dễ",
        Level.Medium => "Trung bình",
        Level.Difficult => "Khó",
        _ => level.ToString()
    };

    private static string QuestionTypeLabel(QuestionType questionType) => questionType switch
    {
        QuestionType.SingleSelection => "Một đáp án",
        QuestionType.MultipleSelection => "Nhiều đáp án",
        _ => questionType.ToString()
    };
}
