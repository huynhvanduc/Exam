using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.QuestionAggregate;

public class AnswerTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("Answer_Constructor.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string content, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new Answer("a1", content, true));
            return;
        }

        var answer = new Answer("a1", content, true);

        Assert.Equal("a1", answer.Id);
        Assert.Equal(content, answer.Content);
        Assert.True(answer.IsCorrect);
    }

    [Fact]
    public void Constructor_DefaultIsCorrect_IsFalse()
    {
        var answer = new Answer("a1", "Nội dung");

        Assert.False(answer.IsCorrect);
    }
}
