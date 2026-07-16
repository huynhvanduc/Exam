using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class CategoryFormDialog : FormDialogBase
{
    [Parameter]
    public CategoryRequest Model { get; set; } = new("", "");

    private string name = "";
    private string urlPath = "";
    private bool isCreate;
    private bool urlPathTouched;

    protected override void OnInitialized()
    {
        name = Model.Name;
        urlPath = Model.UrlPath;
        isCreate = string.IsNullOrEmpty(Model.UrlPath);
    }

    private void OnNameChanged(string? value)
    {
        name = value ?? "";
        if (isCreate && !urlPathTouched)
            urlPath = Slugify(name);
    }

    private void OnUrlPathChanged(string? value)
    {
        urlPath = value ?? "";
        urlPathTouched = true;
    }

    private async Task Submit()
    {
        if (!await ValidateAsync())
            return;

        Dialog.Close(AppDialogResult.Ok(new CategoryRequest(name, urlPath)));
    }

    // Bỏ dấu tiếng Việt qua Unicode normalization (NFD tách dấu ra khỏi ký tự gốc rồi lọc bỏ), xử lý
    // riêng "đ"/"Đ" vì ký tự này không tách dấu qua NFD như các nguyên âm có dấu khác.
    private static string Slugify(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd').Replace('Đ', 'D')
            .ToLowerInvariant();

        slug = Regex.Replace(slug, "[^a-z0-9]+", "-");
        return slug.Trim('-');
    }
}
