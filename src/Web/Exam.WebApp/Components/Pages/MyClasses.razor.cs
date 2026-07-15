using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages;

public partial class MyClasses : PageBase
{
    [Parameter] public string? Id { get; set; }

    private IReadOnlyCollection<ClassRoomDto>? classes;
    private ClassRoomDetailDto? selected;
    private string joinCode = "";

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            selected = null;
            return;
        }

        if (selected?.Id == Id)
            return;

        await ExecuteAsync(async () => selected = await Api.GetClassByIdAsync(Id), "Không tải được lớp học");
    }

    private Task LoadAsync() => ExecuteAsync(async () => classes = await Api.GetMyClassesAsync(), "Không tải được danh sách lớp");

    private void SelectClass(string id) => Navigation.NavigateTo($"/my-classes/{id}");

    private Task JoinAsync() => ExecuteAsync(async () =>
    {
        await Api.JoinClassAsync(joinCode);
        joinCode = "";
        await LoadAsync();
    }, "Không tham gia được lớp (kiểm tra lại mã lớp)", "Đã tham gia lớp.");
}
