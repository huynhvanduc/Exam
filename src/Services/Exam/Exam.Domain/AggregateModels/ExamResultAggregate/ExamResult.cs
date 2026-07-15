using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class ExamResult : Entity, IAggregateRoot
{
    private List<QuestionResult> _questionResults = new();
    private List<string> _questionIds = new();
    private List<DraftAnswer> _draftAnswers = new();

    public string ExamId { get; private set; }

    public string ExamTitle { get; private set; }

    public string UserId { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public IReadOnlyCollection<string> QuestionIds
    {
        get => _questionIds;
        private set => _questionIds = value?.ToList() ?? new List<string>();
    }

    public IReadOnlyCollection<QuestionResult> QuestionResults
    {
        get => _questionResults;
        private set => _questionResults = value?.ToList() ?? new List<QuestionResult>();
    }

    public IReadOnlyCollection<DraftAnswer> DraftAnswers
    {
        get => _draftAnswers;
        private set => _draftAnswers = value?.ToList() ?? new List<DraftAnswer>();
    }

    public TimeSpan? Duration { get; private set; }

    public DateTime? Deadline => Duration.HasValue ? ExamStartDate.Add(Duration.Value) : null;

    public int CorrectQuestionCount { get; private set; }

    // Thang điểm 10 chuẩn học vụ Việt Nam: (số câu đúng / tổng số câu) × 10, làm tròn 2 chữ số thập phân.
    // Guard chia-cho-0 dù Publish() đã chặn đề rỗng - tránh ném DivideByZeroException khó hiểu.
    public decimal TotalScore => _questionResults.Count == 0
        ? 0m
        : Math.Round((decimal)CorrectQuestionCount / _questionResults.Count * 10m, 2);

    // Hằng số thang điểm - giữ làm property tiện lợi để binding "@TotalScore/@MaxPossibleScore điểm" không cần sửa.
    public decimal MaxPossibleScore => 10m;

    public DateTime ExamStartDate { get; private set; }

    public DateTime? ExamFinishDate { get; private set; }

    public bool? Passed { get; private set; }

    public bool Finished { get; private set; }

    private ExamResult()
    {
    }

    public ExamResult(string userId, string examId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ExamDomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(examId))
            throw new ExamDomainException("ExamId is required.");

        UserId = userId;
        ExamId = examId;
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

    public void SetDuration(TimeSpan? duration)
    {
        Duration = duration;
    }

    public bool IsExpired(DateTime at) => !Finished && Deadline.HasValue && at >= Deadline.Value;

    public void RecordAnswer(string questionId, IEnumerable<string> selectedAnswerIds)
    {
        if (Finished)
            throw new ExamDomainException("Cannot record an answer after the exam has finished.");

        if (!_questionIds.Contains(questionId))
            throw new ExamDomainException("This question is not part of the current exam attempt.");

        var ids = selectedAnswerIds?.ToList() ?? new List<string>();

        _draftAnswers.RemoveAll(d => d.QuestionId == questionId);
        _draftAnswers.Add(new DraftAnswer(questionId, ids));
    }

    public void AssignQuestions(IEnumerable<string> questionIds)
    {
        if (Finished)
            throw new ExamDomainException("Cannot assign questions after the exam has finished.");

        if (_questionIds.Count > 0)
            throw new ExamDomainException("Questions have already been assigned to this attempt.");

        var ids = questionIds?.ToList() ?? new List<string>();

        if (ids.Count == 0)
            throw new ExamDomainException("At least one question must be assigned to this attempt.");

        _questionIds = ids;
    }

    public void AddQuestionResult(QuestionResult questionResult)
    {
        if (Finished)
            throw new ExamDomainException("Cannot add a question result after the exam has finished.");

        _questionResults.Add(questionResult);
    }

    public void Finish(decimal minimumPassingScore)
    {
        if (Finished)
            throw new ExamDomainException("Exam result is already finished.");

        CorrectQuestionCount = _questionResults.Count(x => x.Result);
        Passed = TotalScore >= minimumPassingScore;
        ExamFinishDate = DateTime.UtcNow;
        Finished = true;
    }

    // Chấm lại 1 bài đã nộp bằng dữ liệu câu hỏi HIỆN TẠI (vd sau khi sửa lại đáp án đúng bị nhập sai) -
    // khác Finish() ở chỗ KHÔNG đổi ExamFinishDate/Finished (giữ nguyên mốc thời gian nộp bài gốc), chỉ
    // thay QuestionResults + điểm số. Chỉ áp dụng cho bài ĐÃ hoàn thành - bài đang làm dở phải tự nộp bình
    // thường qua Finish().
    public void Regrade(IReadOnlyCollection<QuestionResult> questionResults, decimal minimumPassingScore)
    {
        if (!Finished)
            throw new ExamDomainException("Only a finished exam result can be regraded.");

        _questionResults = questionResults?.ToList() ?? new List<QuestionResult>();
        CorrectQuestionCount = _questionResults.Count(x => x.Result);
        Passed = TotalScore >= minimumPassingScore;
    }
}
