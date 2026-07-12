namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class DraftAnswer
{
    public string QuestionId { get; private set; }

    public IReadOnlyCollection<string> SelectedAnswerIds { get; private set; }

    private DraftAnswer()
    {
    }

    public DraftAnswer(string questionId, IReadOnlyCollection<string> selectedAnswerIds)
    {
        QuestionId = questionId;
        SelectedAnswerIds = selectedAnswerIds ?? new List<string>();
    }
}
