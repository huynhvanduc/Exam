using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class QuestionFormDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string CategoryId { get; set; } = "";

    [Parameter]
    public QuestionDto? Model { get; set; }

    private MudForm form = null!;
    private string content = "";
    private QuestionType questionType = QuestionType.SingleSelection;
    private Level level = Level.Easy;
    private int points = 1;
    private string explain = "";
    private List<AnswerRow> answers = [];
    private int singleCorrectIndex = -1;
    private string? answerError;

    protected override void OnInitialized()
    {
        if (Model != null)
        {
            content = Model.Content;
            questionType = Model.QuestionType;
            level = Model.Level;
            points = Model.Points;
            explain = Model.Explain;
            answers = Model.Answers.Select(a => new AnswerRow { Content = a.Content, IsCorrect = a.IsCorrect }).ToList();
            singleCorrectIndex = answers.FindIndex(a => a.IsCorrect);
        }
        else
        {
            answers = [new AnswerRow(), new AnswerRow()];
        }
    }

    private void AddAnswer() => answers.Add(new AnswerRow());

    private void RemoveAnswer(int index)
    {
        answers.RemoveAt(index);
        if (questionType == QuestionType.SingleSelection)
            singleCorrectIndex = answers.FindIndex(a => a.IsCorrect);
    }

    private void OnQuestionTypeChanged()
    {
        foreach (var answer in answers)
            answer.IsCorrect = false;
        singleCorrectIndex = -1;
    }

    private void OnSingleCorrectChanged(int index)
    {
        singleCorrectIndex = index;
        for (var i = 0; i < answers.Count; i++)
            answers[i].IsCorrect = i == index;
    }

    private async Task Submit()
    {
        await form.ValidateAsync();
        if (!form.IsValid)
            return;

        answerError = null;

        var nonEmptyAnswers = answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).ToList();
        if (nonEmptyAnswers.Count < 2)
        {
            answerError = "Cần ít nhất 2 đáp án.";
            return;
        }

        if (!nonEmptyAnswers.Any(a => a.IsCorrect))
        {
            answerError = "Phải chọn ít nhất 1 đáp án đúng.";
            return;
        }

        var request = new QuestionRequest(
            content,
            questionType,
            level,
            CategoryId,
            nonEmptyAnswers.Select(a => new AnswerInput(a.Content, a.IsCorrect)).ToList(),
            explain,
            points);

        MudDialog.Close(DialogResult.Ok(request));
    }

    private void Cancel() => MudDialog.Cancel();

    private class AnswerRow
    {
        public string Content { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
