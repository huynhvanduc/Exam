using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.ExamAggregate;

public class ExamCompositionCellTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("ExamCompositionCell_Constructor.csv",
        CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(int count, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, count));
            return;
        }

        var cell = new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, count);

        Assert.Equal(Level.Easy, cell.Level);
        Assert.Equal(QuestionType.SingleSelection, cell.QuestionType);
        Assert.Equal(count, cell.Count);
    }
}
