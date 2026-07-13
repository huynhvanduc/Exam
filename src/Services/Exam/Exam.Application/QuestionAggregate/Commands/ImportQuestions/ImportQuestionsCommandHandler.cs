using ClosedXML.Excel;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;
using MongoDB.Bson;

namespace Exam.Application.QuestionAggregate.Commands.ImportQuestions;

public class ImportQuestionsCommandHandler : IRequestHandler<ImportQuestionsCommand, ImportQuestionsResultDto>
{
    private const int HeaderRowNumber = 1;

    private readonly IQuestionRepository _questionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ImportQuestionsCommandHandler(IQuestionRepository questionRepository, ICategoryRepository categoryRepository)
    {
        _questionRepository = questionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ImportQuestionsResultDto> Handle(ImportQuestionsCommand request, CancellationToken cancellationToken)
    {
        var categoryByName = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in await _categoryRepository.GetAllAsync(cancellationToken))
            categoryByName.TryAdd(category.Name.Trim(), category);

        using var workbook = new XLWorkbook(new MemoryStream(request.FileContent));
        var worksheet = workbook.Worksheets.First();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRowNumber;

        var errors = new List<ImportQuestionRowError>();
        var questionsToInsert = new List<Question>();
        var totalRows = 0;

        for (var rowNumber = HeaderRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var categoryName = row.Cell(1).GetString().Trim();
            var levelText = row.Cell(2).GetString().Trim();
            var questionTypeText = row.Cell(3).GetString().Trim();
            var content = row.Cell(4).GetString().Trim();
            var answerA = row.Cell(5).GetString().Trim();
            var answerB = row.Cell(6).GetString().Trim();
            var answerC = row.Cell(7).GetString().Trim();
            var answerD = row.Cell(8).GetString().Trim();
            var correctText = row.Cell(9).GetString().Trim();
            var explain = row.Cell(10).GetString().Trim();
            var pointsText = row.Cell(11).GetString().Trim();

            // Bỏ qua dòng trống hoàn toàn (thường gặp ở cuối file).
            if (string.IsNullOrWhiteSpace(categoryName) && string.IsNullOrWhiteSpace(content))
                continue;

            totalRows++;
            var rowErrors = new List<string>();

            Category? category = null;
            if (string.IsNullOrWhiteSpace(categoryName))
                rowErrors.Add("Thiếu tên môn học.");
            else if (!categoryByName.TryGetValue(categoryName, out category))
                rowErrors.Add($"Không tìm thấy môn học '{categoryName}'.");

            var level = ParseLevel(levelText);
            if (level == null)
                rowErrors.Add("Mức độ không hợp lệ (phải là Dễ/Trung bình/Khó).");

            var questionType = ParseQuestionType(questionTypeText);
            if (questionType == null)
                rowErrors.Add("Loại câu hỏi không hợp lệ (phải là Một đáp án/Nhiều đáp án).");

            if (string.IsNullOrWhiteSpace(content))
                rowErrors.Add("Thiếu nội dung câu hỏi.");

            if (string.IsNullOrWhiteSpace(answerA) || string.IsNullOrWhiteSpace(answerB))
                rowErrors.Add("Đáp án A và B là bắt buộc.");

            var answerLetters = new[] { ('A', answerA), ('B', answerB), ('C', answerC), ('D', answerD) }
                .Where(a => !string.IsNullOrWhiteSpace(a.Item2))
                .ToList();

            var correctLetters = correctText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => char.ToUpperInvariant(s[0]))
                .ToHashSet();

            if (correctLetters.Count == 0)
            {
                rowErrors.Add("Thiếu đáp án đúng.");
            }
            else
            {
                var invalidLetters = correctLetters.Where(l => answerLetters.All(a => a.Item1 != l)).ToList();
                if (invalidLetters.Count > 0)
                    rowErrors.Add($"Đáp án đúng tham chiếu đáp án không tồn tại: {string.Join(", ", invalidLetters)}.");
                else if (questionType == QuestionType.SingleSelection && correctLetters.Count > 1)
                    rowErrors.Add("Câu hỏi 'Một đáp án' chỉ được có đúng 1 đáp án đúng.");
            }

            var points = 1;
            if (!string.IsNullOrWhiteSpace(pointsText) && (!int.TryParse(pointsText, out points) || points <= 0))
                rowErrors.Add("Điểm không hợp lệ (phải là số nguyên dương).");

            if (rowErrors.Count > 0)
            {
                errors.Add(new ImportQuestionRowError(rowNumber, string.Join(" ", rowErrors)));
                continue;
            }

            var answers = answerLetters
                .Select(a => new Answer(ObjectId.GenerateNewId().ToString(), a.Item2, correctLetters.Contains(a.Item1)))
                .ToList();

            questionsToInsert.Add(new Question(null!, content, questionType!.Value, level!.Value, category!.Id,
                answers, explain, points, request.OwnerUserId, category.Name));
        }

        foreach (var question in questionsToInsert)
            await _questionRepository.InsertAsync(question, cancellationToken);

        return new ImportQuestionsResultDto(totalRows, questionsToInsert.Count, errors);
    }

    private static Level? ParseLevel(string text) => text switch
    {
        "Dễ" => Level.Easy,
        "Trung bình" => Level.Medium,
        "Khó" => Level.Difficult,
        _ => null
    };

    private static QuestionType? ParseQuestionType(string text) => text switch
    {
        "Một đáp án" => QuestionType.SingleSelection,
        "Nhiều đáp án" => QuestionType.MultipleSelection,
        _ => null
    };
}
