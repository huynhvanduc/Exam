using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamAggregate;

public class Exam : Entity, IAggregateRoot
{
    private List<string> _questionIds = new();

    public string Name { get; private set; }

    public string ShortDesc { get; private set; }

    public string Content { get; private set; }

    public TimeSpan Duration { get; private set; }

    public Level Level { get; private set; }

    public DateTime DateCreated { get; private set; }

    public string OwnerUserId { get; private set; }

    public int MinimumPassingScore { get; private set; }

    public bool IsTimeRestricted { get; private set; }

    public string CategoryId { get; private set; }

    public string CategoryName { get; private set; }

    public ExamStatus Status { get; private set; }

    public QuestionSelectionMode QuestionSelectionMode { get; private set; } = QuestionSelectionMode.Fixed;

    public string PoolCategoryId { get; private set; }

    public int PoolQuestionCount { get; private set; }

    public DateTime? AvailableFrom { get; private set; }

    public DateTime? AvailableTo { get; private set; }

    public decimal NegativeMarkingRatio { get; private set; }

    public IReadOnlyCollection<string> QuestionIds
    {
        get => _questionIds;
        private set => _questionIds = value?.ToList() ?? new List<string>();
    }

    public int NumberOfQuestions => QuestionSelectionMode == QuestionSelectionMode.Pool
        ? PoolQuestionCount
        : _questionIds.Count;

    private Exam()
    {
    }

    public Exam(string name, string shortDesc, string content, TimeSpan duration, Level level, string ownerUserId,
        string categoryId, string categoryName, bool isTimeRestricted, int minimumPassingScore)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0)
            throw new ExamDomainException("Minimum passing score must not be negative.");

        Name = name;
        ShortDesc = shortDesc;
        Content = content;
        Duration = duration;
        Level = level;
        OwnerUserId = ownerUserId;
        CategoryId = categoryId;
        CategoryName = categoryName;
        IsTimeRestricted = isTimeRestricted;
        MinimumPassingScore = minimumPassingScore;
        DateCreated = DateTime.UtcNow;
        Status = ExamStatus.Draft;
    }

    public void UpdateDetails(string name, string shortDesc, string content, TimeSpan duration, Level level,
        string categoryId, string categoryName, bool isTimeRestricted, int minimumPassingScore)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Exam name is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Exam category is required.");

        if (minimumPassingScore < 0)
            throw new ExamDomainException("Minimum passing score must not be negative.");

        Name = name;
        ShortDesc = shortDesc;
        Content = content;
        Duration = duration;
        Level = level;
        CategoryId = categoryId;
        CategoryName = categoryName;
        IsTimeRestricted = isTimeRestricted;
        MinimumPassingScore = minimumPassingScore;
    }

    public void AddQuestion(string questionId)
    {
        EnsureEditable();

        if (QuestionSelectionMode == QuestionSelectionMode.Pool)
            throw new ExamDomainException("Cannot add a fixed question while the exam uses a question pool.");

        if (string.IsNullOrWhiteSpace(questionId))
            throw new ExamDomainException("Question id is required.");

        if (_questionIds.Contains(questionId))
            throw new ExamDomainException("Question already added to this exam.");

        _questionIds.Add(questionId);
    }

    public void RemoveQuestion(string questionId)
    {
        EnsureEditable();

        _questionIds.Remove(questionId);
    }

    public void ConfigureQuestionPool(string poolCategoryId, int questionCount)
    {
        EnsureEditable();

        if (string.IsNullOrWhiteSpace(poolCategoryId))
            throw new ExamDomainException("Pool category id is required.");

        if (questionCount <= 0)
            throw new ExamDomainException("Pool question count must be greater than zero.");

        if (_questionIds.Count > 0)
            throw new ExamDomainException("Cannot configure a question pool when fixed questions have already been added.");

        QuestionSelectionMode = QuestionSelectionMode.Pool;
        PoolCategoryId = poolCategoryId;
        PoolQuestionCount = questionCount;
    }

    public void ScheduleAvailability(DateTime? availableFrom, DateTime? availableTo)
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("Cannot change the availability window of an archived exam.");

        if (availableFrom.HasValue && availableTo.HasValue && availableTo <= availableFrom)
            throw new ExamDomainException("Available-to must be later than available-from.");

        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
    }

    public bool IsAvailable(DateTime at)
    {
        if (Status != ExamStatus.Published)
            return false;

        if (AvailableFrom.HasValue && at < AvailableFrom.Value)
            return false;

        if (AvailableTo.HasValue && at > AvailableTo.Value)
            return false;

        return true;
    }

    public void ConfigureNegativeMarking(decimal ratio)
    {
        EnsureEditable();

        if (ratio < 0 || ratio > 1)
            throw new ExamDomainException("Negative marking ratio must be between 0 and 1.");

        NegativeMarkingRatio = ratio;
    }

    public void Publish()
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("An archived exam cannot be published.");

        if (Status == ExamStatus.Published)
            throw new ExamDomainException("Exam is already published.");

        if (NumberOfQuestions == 0)
            throw new ExamDomainException("Cannot publish an exam with no questions.");

        Status = ExamStatus.Published;
    }

    public void Unpublish()
    {
        if (Status != ExamStatus.Published)
            throw new ExamDomainException("Only a published exam can be unpublished.");

        Status = ExamStatus.Draft;
    }

    public void Archive()
    {
        if (Status == ExamStatus.Archived)
            throw new ExamDomainException("Exam is already archived.");

        Status = ExamStatus.Archived;
    }

    private void EnsureEditable()
    {
        if (Status != ExamStatus.Draft)
            throw new ExamDomainException("Questions can only be modified while the exam is in draft status.");
    }
}
