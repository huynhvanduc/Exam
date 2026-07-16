using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.ExamResultAggregate;

public class AnswerResultTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("AnswerResult_Constructor.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string content, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new AnswerResult("a1", content, true, true));
            return;
        }

        var answerResult = new AnswerResult("a1", content, true, true);

        Assert.Equal("a1", answerResult.Id);
        Assert.Equal(content, answerResult.Content);
        Assert.True(answerResult.UserChosen);
        Assert.True(answerResult.IsCorrect);
    }
}
