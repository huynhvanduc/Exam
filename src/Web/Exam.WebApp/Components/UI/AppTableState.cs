namespace Exam.WebApp.Components.UI;

public sealed class AppTableState
{
    public int Page { get; set; }
    public int PageSize { get; set; } = 10;
}

public sealed class AppTableData<TItem>
{
    public IReadOnlyCollection<TItem> Items { get; set; } = [];
    public int TotalItems { get; set; }
}
