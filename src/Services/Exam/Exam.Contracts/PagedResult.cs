namespace Exam.Contracts;

public record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long TotalCount);

public static class PagedResultFactory
{
    public static async Task<PagedResult<T>> CreateAsync<T>(int page, int pageSize,
        Func<int, int, Task<IReadOnlyCollection<T>>> fetch, Func<Task<long>> count)
    {
        var skip = (page - 1) * pageSize;
        var items = await fetch(skip, pageSize);
        var totalCount = await count();
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
