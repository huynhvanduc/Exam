using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using Exam.Domain.UnitTests.Fakes;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;
using ExamCompositionCell = Exam.Domain.AggregateModels.ExamAggregate.ExamCompositionCell;

namespace Exam.Domain.UnitTests.Services;

public class ExamQuestionPoolServiceTests
{
    private static ExamEntity CreateExamWithComposition(params ExamCompositionCell[] cells)
    {
        var exam = new ExamEntity("Đề kiểm tra", "desc", "content", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Category", true, 5m);
        exam.ConfigureComposition(cells);
        return exam;
    }

    private static List<Question> BuildCandidates(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Question($"q{i}", $"Câu hỏi {i}", QuestionType.SingleSelection, Level.Easy, "cat-1",
                [new Answer("a1", "A", true)], ""))
            .ToList();

    public static IEnumerable<object?[]> DrawQuestionIdsCases() => CsvTestData.Read("ExamQuestionPoolService_DrawQuestionIds.csv",
        CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(DrawQuestionIdsCases))]
    public async Task DrawQuestionIdsAsync(int candidateCount, int requestedCount, bool shouldThrow)
    {
        var exam = CreateExamWithComposition(new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, requestedCount));
        var questionRepository = new FakeQuestionRepository { CategoryLevelTypeResult = BuildCandidates(candidateCount) };
        var service = new ExamQuestionPoolService(questionRepository);

        if (shouldThrow)
        {
            await Assert.ThrowsAsync<ExamDomainException>(() => service.DrawQuestionIdsAsync(exam));
            return;
        }

        var drawn = await service.DrawQuestionIdsAsync(exam);

        Assert.Equal(requestedCount, drawn.Count);
        Assert.Equal(drawn.Count, drawn.Distinct().Count());
    }

    [Fact]
    public async Task DrawQuestionIdsAsync_CellWithZeroCount_IsSkipped()
    {
        var exam = CreateExamWithComposition(
            new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 0),
            new ExamCompositionCell(Level.Medium, QuestionType.MultipleSelection, 2));
        var questionRepository = new FakeQuestionRepository { CategoryLevelTypeResult = BuildCandidates(5) };
        var service = new ExamQuestionPoolService(questionRepository);

        var drawn = await service.DrawQuestionIdsAsync(exam);

        Assert.Equal(2, drawn.Count);
    }
}
