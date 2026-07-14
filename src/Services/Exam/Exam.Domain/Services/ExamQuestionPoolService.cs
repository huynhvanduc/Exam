using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Domain.Services;

public class ExamQuestionPoolService
{
    private readonly IQuestionRepository _questionRepository;

    public ExamQuestionPoolService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<IReadOnlyCollection<string>> DrawQuestionIdsAsync(ExamEntity exam, CancellationToken cancellationToken = default)
    {
        var drawn = new List<string>();

        foreach (var cell in exam.Composition)
        {
            if (cell.Count == 0)
                continue;

            var candidates = await _questionRepository.GetByCategoryLevelTypeAsync(
                exam.CategoryId, cell.Level, cell.QuestionType, cancellationToken);

            if (candidates.Count < cell.Count)
                throw new ExamDomainException(
                    $"Không đủ câu hỏi mức '{LevelLabel(cell.Level)}' loại '{QuestionTypeLabel(cell.QuestionType)}' " +
                    $"trong ngân hàng để rút {cell.Count} câu (chỉ có {candidates.Count}).");

            var pool = candidates.Select(q => q.Id).ToArray();
            Random.Shared.Shuffle(pool);
            drawn.AddRange(pool.Take(cell.Count));
        }

        return drawn;
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
