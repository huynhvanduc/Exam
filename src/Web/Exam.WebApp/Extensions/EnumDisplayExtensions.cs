using Exam.WebApp.Components.UI;

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

    public static AppStatusPillVariant ToPillVariant(this ExamStatus status) => status switch
    {
        ExamStatus.Draft => AppStatusPillVariant.Warning,
        ExamStatus.Published => AppStatusPillVariant.Success,
        ExamStatus.Archived => AppStatusPillVariant.Danger,
        _ => AppStatusPillVariant.Info
    };
}
