namespace Exam.API.Extensions;

public static class PagingDefaults
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultPageSize) =>
        (page <= 0 ? 1 : page, pageSize <= 0 ? defaultPageSize : pageSize);
}
