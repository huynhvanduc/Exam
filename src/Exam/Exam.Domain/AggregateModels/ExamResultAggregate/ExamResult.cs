using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class ExamResult : Entity, IAggregateRoot
{
    private List<QuestionResult> _questionResults = new();

    public string ExamId { get; private set; }

    public string ExamTitle { get; private set; }

    public string UserId { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public IReadOnlyCollection<QuestionResult> QuestionResults
    {
        get => _questionResults;
        private set => _questionResults = value?.ToList() ?? new List<QuestionResult>();
    }

    public int CorrectQuestionCount { get; private set; }

    public decimal NegativeMarkingRatio { get; private set; }

    public decimal TotalScore => _questionResults.Sum(ScoreFor);

    public int MaxPossibleScore => _questionResults.Sum(x => x.Points);

    public DateTime ExamStartDate { get; private set; }

    public DateTime? ExamFinishDate { get; private set; }

    public bool? Passed { get; private set; }

    public bool Finished { get; private set; }

    private ExamResult()
    {
    }

    public ExamResult(string userId, string examId, decimal negativeMarkingRatio = 0m)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ExamDomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(examId))
            throw new ExamDomainException("ExamId is required.");

        if (negativeMarkingRatio < 0 || negativeMarkingRatio > 1)
            throw new ExamDomainException("Negative marking ratio must be between 0 and 1.");

        UserId = userId;
        ExamId = examId;
        NegativeMarkingRatio = negativeMarkingRatio;
        ExamStartDate = DateTime.UtcNow;
        Finished = false;
    }

    public void SetExamTitle(string examTitle)
    {
        if (string.IsNullOrWhiteSpace(examTitle))
            throw new ExamDomainException("Exam title is required.");

        ExamTitle = examTitle;
    }

    public void SetUserInfo(string email, string fullName)
    {
        Email = email;
        FullName = fullName;
    }

    public void AddQuestionResult(QuestionResult questionResult)
    {
        if (Finished)
            throw new ExamDomainException("Cannot add a question result after the exam has finished.");

        _questionResults.Add(questionResult);
    }

    public void Finish(int minimumPassingScore)
    {
        if (Finished)
            throw new ExamDomainException("Exam result is already finished.");

        CorrectQuestionCount = _questionResults.Count(x => x.Result);
        Passed = TotalScore >= minimumPassingScore;
        ExamFinishDate = DateTime.UtcNow;
        Finished = true;
    }

    private decimal ScoreFor(QuestionResult questionResult)
    {
        if (questionResult.Result)
            return questionResult.Points;

        if (questionResult.IsAnswered)
            return -(questionResult.Points * NegativeMarkingRatio);

        return 0m;
    }
}
