using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.ExamResultAggregate;

public class QuestionResultTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("QuestionResult_Constructor.csv",
        CsvTestData.Str, CsvTestData.Bool, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string content, bool hasAnswers, bool shouldThrow)
    {
        var answers = hasAnswers
            ? new[] { new AnswerResult("a1", "A", true, true) }
            : [];

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() =>
                new QuestionResult("q1", content, QuestionType.SingleSelection, Level.Easy, answers, "giải thích"));
            return;
        }

        var questionResult = new QuestionResult("q1", content, QuestionType.SingleSelection, Level.Easy, answers, "giải thích");

        Assert.Equal(content, questionResult.Content);
        Assert.Equal("q1", questionResult.Id);
    }

    [Fact]
    public void Constructor_NullAnswers_Throws()
    {
        Assert.Throws<ExamDomainException>(() =>
            new QuestionResult("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, null!, "giải thích"));
    }

    [Fact]
    public void Result_AllAnswersMatchCorrectness_ReturnsTrue()
    {
        var answers = new[]
        {
            new AnswerResult("a1", "A", true, true),
            new AnswerResult("a2", "B", false, false)
        };

        var questionResult = new QuestionResult("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, answers, "");

        Assert.True(questionResult.Result);
    }

    [Fact]
    public void Result_WrongChoice_ReturnsFalse()
    {
        var answers = new[]
        {
            new AnswerResult("a1", "A", false, true),
            new AnswerResult("a2", "B", true, false)
        };

        var questionResult = new QuestionResult("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, answers, "");

        Assert.False(questionResult.Result);
    }

    [Fact]
    public void Result_MultiSelect_PartiallyCorrect_ReturnsFalse()
    {
        var answers = new[]
        {
            new AnswerResult("a1", "A", true, true),
            new AnswerResult("a2", "B", false, true), // đáp án đúng nhưng không chọn -> sai toàn bộ
            new AnswerResult("a3", "C", false, false)
        };

        var questionResult = new QuestionResult("q1", "Câu hỏi 1", QuestionType.MultipleSelection, Level.Easy, answers, "");

        Assert.False(questionResult.Result);
    }

    [Fact]
    public void IsAnswered_NoAnswerChosen_ReturnsFalse()
    {
        var answers = new[] { new AnswerResult("a1", "A", null, true) };

        var questionResult = new QuestionResult("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, answers, "");

        Assert.False(questionResult.IsAnswered);
    }

    [Fact]
    public void IsAnswered_OneAnswerChosen_ReturnsTrue()
    {
        var answers = new[] { new AnswerResult("a1", "A", true, true) };

        var questionResult = new QuestionResult("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, answers, "");

        Assert.True(questionResult.IsAnswered);
    }
}
