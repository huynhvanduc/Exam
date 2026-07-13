using MudBlazor;

namespace Exam.WebApp.Components.Pages;

public partial class MyHistory : PageBase
{
    private MudTable<ExamResultSummaryDto>? table;

    private Task<TableData<ExamResultSummaryDto>> LoadServerData(TableState state, CancellationToken cancellationToken) =>
        LoadTableDataAsync(async () =>
        {
            var result = await Api.GetMyExamHistoryAsync(state.Page + 1, state.PageSize, cancellationToken);
            return new TableData<ExamResultSummaryDto> { Items = result.Items, TotalItems = (int)result.TotalCount };
        }, "Không tải được lịch sử làm bài");
}
