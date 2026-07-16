using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using Exam.Domain.UnitTests.Fakes;

namespace Exam.Domain.UnitTests.Services;

public class CategoryDeletionGuardTests
{
    public static IEnumerable<object?[]> EnsureCanDeleteCases() => CsvTestData.Read("CategoryDeletionGuard_EnsureCanDelete.csv",
        CsvTestData.Bool, CsvTestData.Bool, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(EnsureCanDeleteCases))]
    public async Task EnsureCanDeleteAsync(bool examExists, bool questionExists, bool shouldThrow)
    {
        var examRepository = new FakeExamRepository { ExistsByCategoryIdResult = examExists };
        var questionRepository = new FakeQuestionRepository { ExistsByCategoryIdResult = questionExists };
        var guard = new CategoryDeletionGuard(examRepository, questionRepository);

        if (shouldThrow)
        {
            await Assert.ThrowsAsync<ExamDomainException>(() => guard.EnsureCanDeleteAsync("cat-1"));
            return;
        }

        var exception = await Record.ExceptionAsync(() => guard.EnsureCanDeleteAsync("cat-1"));

        Assert.Null(exception);
    }
}
