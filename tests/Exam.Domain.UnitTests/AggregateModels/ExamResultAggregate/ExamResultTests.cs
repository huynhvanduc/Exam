using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.ExamResultAggregate;

public class ExamResultTests
{
    private static QuestionResult CorrectQuestionResult(string id) =>
        new(id, "Nội dung", QuestionType.SingleSelection, Level.Easy,
            [new AnswerResult("a1", "A", true, true)], "giải thích");

    private static QuestionResult IncorrectQuestionResult(string id) =>
        new(id, "Nội dung", QuestionType.SingleSelection, Level.Easy,
            [new AnswerResult("a1", "A", false, true)], "giải thích");

    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("ExamResult_Constructor.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string userId, string examId, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new ExamResult(userId, examId));
            return;
        }

        var examResult = new ExamResult(userId, examId);

        Assert.Equal(userId, examResult.UserId);
        Assert.Equal(examId, examResult.ExamId);
        Assert.False(examResult.Finished);
        Assert.Equal(0m, examResult.TotalScore);
    }

    public static IEnumerable<object?[]> SetExamTitleCases() => CsvTestData.Read("ExamResult_SetExamTitle.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(SetExamTitleCases))]
    public void SetExamTitle(string examTitle, bool shouldThrow)
    {
        var examResult = new ExamResult("user-1", "exam-1");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => examResult.SetExamTitle(examTitle));
            return;
        }

        examResult.SetExamTitle(examTitle);

        Assert.Equal(examTitle, examResult.ExamTitle);
    }

    [Fact]
    public void SetUserInfo_AssignsEmailAndFullName()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        examResult.SetUserInfo("user@example.com", "Nguyễn Văn A");

        Assert.Equal("user@example.com", examResult.Email);
        Assert.Equal("Nguyễn Văn A", examResult.FullName);
    }

    [Fact]
    public void SetDuration_AssignsDuration_AndAffectsDeadline()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        examResult.SetDuration(TimeSpan.FromMinutes(45));

        Assert.Equal(TimeSpan.FromMinutes(45), examResult.Duration);
        Assert.Equal(examResult.ExamStartDate.AddMinutes(45), examResult.Deadline);
    }

    [Fact]
    public void Deadline_NoDuration_ReturnsNull()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        Assert.Null(examResult.Deadline);
    }

    public static IEnumerable<object?[]> IsExpiredCases() => CsvTestData.Read("ExamResult_IsExpired.csv",
        CsvTestData.NullableInt, CsvTestData.Int, CsvTestData.Bool, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(IsExpiredCases))]
    public void IsExpired(int? durationMinutes, int minutesAfterStart, bool finished, bool expectedExpired)
    {
        var examResult = new ExamResult("user-1", "exam-1");
        if (durationMinutes.HasValue)
            examResult.SetDuration(TimeSpan.FromMinutes(durationMinutes.Value));
        if (finished)
        {
            examResult.AssignQuestions(["q1"]);
            examResult.AddQuestionResult(CorrectQuestionResult("q1"));
            examResult.Finish(0m);
        }

        var result = examResult.IsExpired(examResult.ExamStartDate.AddMinutes(minutesAfterStart));

        Assert.Equal(expectedExpired, result);
    }

    public static IEnumerable<object?[]> RecordAnswerCases() => CsvTestData.Read("ExamResult_RecordAnswer.csv",
        CsvTestData.Str, CsvTestData.Bool, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(RecordAnswerCases))]
    public void RecordAnswer(string questionId, bool finished, bool shouldThrow)
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1", "q2"]);
        if (finished)
        {
            examResult.AddQuestionResult(CorrectQuestionResult("q1"));
            examResult.AddQuestionResult(CorrectQuestionResult("q2"));
            examResult.Finish(0m);
        }

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => examResult.RecordAnswer(questionId, ["a1"]));
            return;
        }

        examResult.RecordAnswer(questionId, ["a1"]);

        Assert.Contains(examResult.DraftAnswers, d => d.QuestionId == questionId);
    }

    [Fact]
    public void RecordAnswer_SameQuestionTwice_ReplacesPreviousAnswer()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);

        examResult.RecordAnswer("q1", ["a1"]);
        examResult.RecordAnswer("q1", ["a2"]);

        var draft = Assert.Single(examResult.DraftAnswers);
        Assert.Equal(["a2"], draft.SelectedAnswerIds);
    }

    [Fact]
    public void AssignQuestions_Valid_Succeeds()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        examResult.AssignQuestions(["q1", "q2"]);

        Assert.Equal(2, examResult.QuestionIds.Count);
    }

    [Fact]
    public void AssignQuestions_Empty_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        Assert.Throws<ExamDomainException>(() => examResult.AssignQuestions([]));
    }

    [Fact]
    public void AssignQuestions_AlreadyAssigned_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);

        Assert.Throws<ExamDomainException>(() => examResult.AssignQuestions(["q2"]));
    }

    [Fact]
    public void AssignQuestions_WhenFinished_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.AddQuestionResult(CorrectQuestionResult("q1"));
        examResult.Finish(0m);

        Assert.Throws<ExamDomainException>(() => examResult.AssignQuestions(["q2"]));
    }

    [Fact]
    public void AddQuestionResult_WhenNotFinished_Succeeds()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);

        examResult.AddQuestionResult(CorrectQuestionResult("q1"));

        Assert.Single(examResult.QuestionResults);
    }

    [Fact]
    public void AddQuestionResult_WhenFinished_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.AddQuestionResult(CorrectQuestionResult("q1"));
        examResult.Finish(0m);

        Assert.Throws<ExamDomainException>(() => examResult.AddQuestionResult(CorrectQuestionResult("q2")));
    }

    public static IEnumerable<object?[]> FinishCases() => CsvTestData.Read("ExamResult_Finish.csv",
        CsvTestData.Int, CsvTestData.Int, CsvTestData.Decimal, CsvTestData.Bool, CsvTestData.Decimal);

    [Theory]
    [MemberData(nameof(FinishCases))]
    public void Finish(int correctCount, int totalCount, decimal minimumPassingScore, bool expectedPassed, decimal expectedTotalScore)
    {
        var examResult = new ExamResult("user-1", "exam-1");
        var questionIds = Enumerable.Range(1, totalCount).Select(i => $"q{i}").ToList();
        examResult.AssignQuestions(questionIds);
        for (var i = 0; i < totalCount; i++)
            examResult.AddQuestionResult(i < correctCount ? CorrectQuestionResult(questionIds[i]) : IncorrectQuestionResult(questionIds[i]));

        examResult.Finish(minimumPassingScore);

        Assert.True(examResult.Finished);
        Assert.NotNull(examResult.ExamFinishDate);
        Assert.Equal(correctCount, examResult.CorrectQuestionCount);
        Assert.Equal(expectedTotalScore, examResult.TotalScore);
        Assert.Equal(expectedPassed, examResult.Passed);
    }

    [Fact]
    public void Finish_WhenAlreadyFinished_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.AddQuestionResult(CorrectQuestionResult("q1"));
        examResult.Finish(0m);

        Assert.Throws<ExamDomainException>(() => examResult.Finish(0m));
    }

    [Fact]
    public void TotalScore_NoQuestionResults_ReturnsZero()
    {
        var examResult = new ExamResult("user-1", "exam-1");

        Assert.Equal(0m, examResult.TotalScore);
    }

    [Fact]
    public void Regrade_WhenFinished_UpdatesScoreWithoutChangingFinishDateOrFinishedFlag()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1", "q2"]);
        examResult.AddQuestionResult(IncorrectQuestionResult("q1"));
        examResult.AddQuestionResult(IncorrectQuestionResult("q2"));
        examResult.Finish(5m);
        var originalFinishDate = examResult.ExamFinishDate;

        examResult.Regrade([CorrectQuestionResult("q1"), CorrectQuestionResult("q2")], 5m);

        Assert.Equal(2, examResult.CorrectQuestionCount);
        Assert.True(examResult.Passed);
        Assert.True(examResult.Finished);
        Assert.Equal(originalFinishDate, examResult.ExamFinishDate);
    }

    [Fact]
    public void Regrade_WhenNotFinished_Throws()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);

        Assert.Throws<ExamDomainException>(() => examResult.Regrade([CorrectQuestionResult("q1")], 5m));
    }
}
