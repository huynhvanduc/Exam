using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ExamFormDialog : FormDialogBase
{
    [Parameter]
    public string CategoryId { get; set; } = "";

    [Parameter]
    public ExamDto? Model { get; set; }

    private string name = "";
    private string shortDesc = "";
    private string content = "";
    private int durationMinutes = 30;
    private Level level = Level.Easy;
    private decimal minimumPassingScore = 5m;
    private bool isTimeRestricted = true;

    private int easySingle;
    private int easyMulti;
    private int mediumSingle;
    private int mediumMulti;
    private int difficultSingle;
    private int difficultMulti;
    private string? compositionError;

    private bool enableAvailability;
    private DateTime? availableFrom;
    private DateTime? availableTo;

    private bool enableMaxAttempts;
    private int maxAttempts = 1;

    // Chỉ khoá cấu hình khi sửa đề thi đã rời trạng thái Nháp - đề mới tạo luôn Draft.
    private bool isLocked => Model != null && Model.Status != ExamStatus.Draft;
    private bool isArchived => Model != null && Model.Status == ExamStatus.Archived;

    private int CompositionTotal => easySingle + easyMulti + mediumSingle + mediumMulti + difficultSingle + difficultMulti;

    protected override void OnInitialized()
    {
        if (Model != null)
        {
            name = Model.Name;
            shortDesc = Model.ShortDesc;
            content = Model.Content;
            durationMinutes = (int)Model.Duration.TotalMinutes;
            level = Model.Level;
            minimumPassingScore = Model.MinimumPassingScore;
            isTimeRestricted = Model.IsTimeRestricted;

            easySingle = CountOf(Level.Easy, QuestionType.SingleSelection);
            easyMulti = CountOf(Level.Easy, QuestionType.MultipleSelection);
            mediumSingle = CountOf(Level.Medium, QuestionType.SingleSelection);
            mediumMulti = CountOf(Level.Medium, QuestionType.MultipleSelection);
            difficultSingle = CountOf(Level.Difficult, QuestionType.SingleSelection);
            difficultMulti = CountOf(Level.Difficult, QuestionType.MultipleSelection);

            enableAvailability = Model.AvailableFrom.HasValue || Model.AvailableTo.HasValue;
            availableFrom = Model.AvailableFrom;
            availableTo = Model.AvailableTo;

            enableMaxAttempts = Model.MaxAttempts.HasValue;
            if (Model.MaxAttempts.HasValue)
                maxAttempts = Model.MaxAttempts.Value;
        }
    }

    private int CountOf(Level level, QuestionType questionType) =>
        Model!.Composition.FirstOrDefault(c => c.Level == level && c.QuestionType == questionType)?.Count ?? 0;

    private ConfigureExamCompositionRequest BuildCompositionRequest()
    {
        var cells = new List<ExamCompositionCellDto>();
        void Add(Level l, QuestionType t, int n)
        {
            if (n > 0)
                cells.Add(new ExamCompositionCellDto(l, t, n));
        }
        Add(Level.Easy, QuestionType.SingleSelection, easySingle);
        Add(Level.Easy, QuestionType.MultipleSelection, easyMulti);
        Add(Level.Medium, QuestionType.SingleSelection, mediumSingle);
        Add(Level.Medium, QuestionType.MultipleSelection, mediumMulti);
        Add(Level.Difficult, QuestionType.SingleSelection, difficultSingle);
        Add(Level.Difficult, QuestionType.MultipleSelection, difficultMulti);
        return new ConfigureExamCompositionRequest(cells);
    }

    private async Task Submit()
    {
        compositionError = null;

        if (!await ValidateAsync())
            return;

        // Ma trận rỗng chỉ chấp nhận khi ĐANG sửa đề đã khoá (không đụng tới cấu hình cũ). Với đề mới hoặc
        // đề Draft đang sửa, phải có ít nhất 1 câu - nếu không, đề không thể publish (NumberOfQuestions == 0).
        if (!isLocked && CompositionTotal == 0)
        {
            compositionError = "Ma trận phải có tổng số câu lớn hơn 0.";
            return;
        }

        var request = new ExamRequest(
            name,
            shortDesc,
            content,
            TimeSpan.FromMinutes(durationMinutes),
            level,
            CategoryId,
            isTimeRestricted,
            minimumPassingScore);

        var availability = enableAvailability ? new ScheduleExamAvailabilityRequest(availableFrom, availableTo) : null;
        var composition = !isLocked && CompositionTotal > 0 ? BuildCompositionRequest() : null;
        var maxAttemptsRequest = enableMaxAttempts ? new ConfigureMaxAttemptsRequest(maxAttempts) : null;

        Dialog.Close(AppDialogResult.Ok(new ExamFormResult(request, availability, composition, maxAttemptsRequest)));
    }
}
