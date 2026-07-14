namespace Exam.WebApp.Extensions;

public static class TextUtils
{
    public static string Truncate(string content, int maxLength) =>
        content.Length <= maxLength ? content : content[..maxLength] + "…";
}
