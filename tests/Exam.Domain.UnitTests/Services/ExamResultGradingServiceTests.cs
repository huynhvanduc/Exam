using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;
using Exam.Domain.UnitTests.Fakes;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Domain.UnitTests.Services;

public class ExamResultGradingServiceTests
{
    private static ExamEntity CreateExam(decimal minimumPassingScore) =>
        new("Đề kiểm tra", "desc", "content", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Category", true, minimumPassingScore);

    private static Question CreateQuestion(string id, string correctAnswerId) =>
        new(id, $"Câu hỏi {id}", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a1", "A", correctAnswerId == "a1"), new Answer("a2", "B", correctAnswerId == "a2")], "");

    [Fact]
    public async Task GradeAndFinishAsync_AlreadyFinished_DoesNothing()
    {
        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.AddQuestionResult(new QuestionResult("q1", "Câu hỏi q1", QuestionType.SingleSelection, Level.Easy,
            [new AnswerResult("a1", "A", true, true)], ""));
        examResult.Finish(0m);
        var finishDate = examResult.ExamFinishDate;

        var service = new ExamResultGradingService(new FakeExamRepository(), new FakeQuestionRepository());
        await service.GradeAndFinishAsync(examResult);

        Assert.Equal(finishDate, examResult.ExamFinishDate);
        Assert.Single(examResult.QuestionResults);
    }

    [Fact]
    public async Task GradeAndFinishAsync_ComputesScoreFromDraftAnswers()
    {
        var exam = CreateExam(5m);
        var examRepository = new FakeExamRepository();
        examRepository.ExamsById["exam-1"] = exam;
        var questionRepository = new FakeQuestionRepository();
        questionRepository.QuestionsById["q1"] = CreateQuestion("q1", "a1");
        questionRepository.QuestionsById["q2"] = CreateQuestion("q2", "a2");

        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1", "q2"]);
        examResult.RecordAnswer("q1", ["a1"]); // đúng
        examResult.RecordAnswer("q2", ["a1"]); // sai (đáp án đúng là a2)

        var service = new ExamResultGradingService(examRepository, questionRepository);
        await service.GradeAndFinishAsync(examResult);

        Assert.True(examResult.Finished);
        Assert.Equal(1, examResult.CorrectQuestionCount);
        Assert.Equal(5.0m, examResult.TotalScore);
        Assert.True(examResult.Passed);
    }

    [Fact]
    public async Task GradeAndFinishAsync_QuestionMissingFromBank_IsSkipped()
    {
        var examRepository = new FakeExamRepository();
        examRepository.ExamsById["exam-1"] = CreateExam(0m);
        var questionRepository = new FakeQuestionRepository();
        questionRepository.QuestionsById["q1"] = CreateQuestion("q1", "a1");
        // "q404" không có trong ngân hàng - đã bị xoá sau khi đã gán vào bài thi.

        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1", "q404"]);
        examResult.RecordAnswer("q1", ["a1"]);

        var service = new ExamResultGradingService(examRepository, questionRepository);
        await service.GradeAndFinishAsync(examResult);

        Assert.Single(examResult.QuestionResults);
    }

    [Fact]
    public async Task GradeAndFinishAsync_ExamNotFound_UsesZeroAsMinimumPassingScore()
    {
        var examRepository = new FakeExamRepository(); // không có exam-1 -> GetByIdAsync trả null
        var questionRepository = new FakeQuestionRepository();
        questionRepository.QuestionsById["q1"] = CreateQuestion("q1", "a1");

        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.RecordAnswer("q1", ["a2"]); // sai

        var service = new ExamResultGradingService(examRepository, questionRepository);
        await service.GradeAndFinishAsync(examResult);

        Assert.True(examResult.Finished);
        Assert.True(examResult.Passed); // 0 điểm nhưng ngưỡng đạt mặc định = 0 -> vẫn coi là đạt
    }

    [Fact]
    public async Task RegradeAsync_UpdatesScore_WithoutChangingFinishedStateOrDate()
    {
        var exam = CreateExam(5m);
        var examRepository = new FakeExamRepository();
        examRepository.ExamsById["exam-1"] = exam;
        var questionRepository = new FakeQuestionRepository();
        questionRepository.QuestionsById["q1"] = CreateQuestion("q1", "a1");

        var examResult = new ExamResult("user-1", "exam-1");
        examResult.AssignQuestions(["q1"]);
        examResult.RecordAnswer("q1", ["a2"]); // sai lúc nộp bài

        var service = new ExamResultGradingService(examRepository, questionRepository);
        await service.GradeAndFinishAsync(examResult);
        Assert.False(examResult.Passed);
        var finishDate = examResult.ExamFinishDate;

        // Giảng viên sửa lại đáp án đúng của câu hỏi (a2 mới là đúng) rồi chấm lại.
        questionRepository.QuestionsById["q1"] = CreateQuestion("q1", "a2");
        await service.RegradeAsync(examResult);

        Assert.Equal(1, examResult.CorrectQuestionCount);
        Assert.True(examResult.Passed);
        Assert.True(examResult.Finished);
        Assert.Equal(finishDate, examResult.ExamFinishDate);
    }
}
