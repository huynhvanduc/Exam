using MudBlazor;

namespace Exam.WebApp.Extensions;

public static class EnumDisplayExtensions
{
    public static string ToLabel(this Level level) => level switch
    {
        Level.Easy => "Dễ",
        Level.Medium => "Trung bình",
        Level.Difficult => "Khó",
        _ => level.ToString()
    };

    public static string ToLabel(this ExamStatus status) => status switch
    {
        ExamStatus.Draft => "Nháp",
        ExamStatus.Published => "Đã xuất bản",
        ExamStatus.Archived => "Lưu trữ",
        _ => status.ToString()
    };

    public static Color ToColor(this ExamStatus status) => status switch
    {
        ExamStatus.Draft => Color.Default,
        ExamStatus.Published => Color.Success,
        ExamStatus.Archived => Color.Dark,
        _ => Color.Default
    };
}
