using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.QuestionAggregate;

public class QuestionTests
{
    private static IReadOnlyCollection<Answer> BuildAnswers(int correctCount, int totalCount) =>
        Enumerable.Range(1, totalCount)
            .Select(i => new Answer($"a{i}", $"Đáp án {i}", i <= correctCount))
            .ToList();

    private static Question CreateValidQuestion() =>
        new("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, "cat-1",
            BuildAnswers(1, 4), "giải thích", "teacher-1", "Category");

    public static IEnumerable<object?[]> ConstructorRequiredCases() => CsvTestData.Read("Question_Constructor_Required.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorRequiredCases))]
    public void Constructor_RequiredFields(string content, string categoryId, bool shouldThrow)
    {
        var answers = BuildAnswers(1, 4);

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() =>
                new Question("q1", content, QuestionType.SingleSelection, Level.Easy, categoryId, answers, "giải thích"));
            return;
        }

        var question = new Question("q1", content, QuestionType.SingleSelection, Level.Easy, categoryId, answers, "giải thích");

        Assert.Equal(content, question.Content);
        Assert.Equal(categoryId, question.CategoryId);
    }

    public static IEnumerable<object?[]> ConstructorAnswersCases() => CsvTestData.Read("Question_Constructor_Answers.csv",
        CsvTestData.Str, CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorAnswersCases))]
    public void Constructor_AnswerRules(string questionTypeStr, int correctCount, int totalCount, bool shouldThrow)
    {
        var questionType = Enum.Parse<QuestionType>(questionTypeStr);
        var answers = BuildAnswers(correctCount, totalCount);

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() =>
                new Question("q1", "Câu hỏi 1", questionType, Level.Easy, "cat-1", answers, "giải thích"));
            return;
        }

        var question = new Question("q1", "Câu hỏi 1", questionType, Level.Easy, "cat-1", answers, "giải thích");

        Assert.Equal(questionType, question.QuestionType);
        Assert.Equal(totalCount, question.Answers.Count);
    }

    [Fact]
    public void Constructor_NullAnswers_Throws()
    {
        Assert.Throws<ExamDomainException>(() =>
            new Question("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, "cat-1", null!, "giải thích"));
    }

    [Fact]
    public void Constructor_EmptyAnswers_Throws()
    {
        Assert.Throws<ExamDomainException>(() =>
            new Question("q1", "Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, "cat-1", [], "giải thích"));
    }

    [Fact]
    public void Update_ValidInput_UpdatesFields()
    {
        var question = CreateValidQuestion();

        question.Update("Câu hỏi mới", QuestionType.MultipleSelection, Level.Difficult, "cat-2", "Category mới",
            BuildAnswers(2, 4), "giải thích mới");

        Assert.Equal("Câu hỏi mới", question.Content);
        Assert.Equal(QuestionType.MultipleSelection, question.QuestionType);
        Assert.Equal("cat-2", question.CategoryId);
    }

    [Fact]
    public void Update_InvalidContent_Throws()
    {
        var question = CreateValidQuestion();

        Assert.Throws<ExamDomainException>(() =>
            question.Update("", QuestionType.SingleSelection, Level.Easy, "cat-1", "Category", BuildAnswers(1, 4), "giải thích"));
    }

    [Fact]
    public void Update_SingleSelectionWithMultipleCorrectAnswers_Throws()
    {
        var question = CreateValidQuestion();

        Assert.Throws<ExamDomainException>(() =>
            question.Update("Câu hỏi 1", QuestionType.SingleSelection, Level.Easy, "cat-1", "Category", BuildAnswers(2, 4), "giải thích"));
    }

    public static IEnumerable<object?[]> ChangeCategoryCases() => CsvTestData.Read("Question_ChangeCategory.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ChangeCategoryCases))]
    public void ChangeCategory(string categoryId, bool shouldThrow)
    {
        var question = CreateValidQuestion();

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => question.ChangeCategory(categoryId, "Category mới"));
            return;
        }

        question.ChangeCategory(categoryId, "Category mới");

        Assert.Equal(categoryId, question.CategoryId);
        Assert.Equal("Category mới", question.CategoryName);
    }
}
