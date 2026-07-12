namespace Exam.Application.Common;

public record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, long TotalCount);
