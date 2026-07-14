using Exam.WebApp.Components.UI;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages;

public partial class MyHistory : PageBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private AppTable<ExamResultSummaryDto>? table;

    private Task<AppTableData<ExamResultSummaryDto>> LoadServerData(AppTableState state, CancellationToken cancellationToken) =>
        LoadAppTableDataAsync(async () =>
        {
            var result = await Api.GetMyExamHistoryAsync(state.Page + 1, state.PageSize, cancellationToken);
            return new AppTableData<ExamResultSummaryDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được lịch sử làm bài");

    private void GoToResult(string attemptId) => Navigation.NavigateTo($"/exams/result/{attemptId}");

    private void GoToTake(string attemptId) => Navigation.NavigateTo($"/exams/take/{attemptId}");
}
