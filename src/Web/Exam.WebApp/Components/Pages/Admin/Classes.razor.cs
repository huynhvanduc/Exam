using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Classes : AdminPageBase
{
    private IReadOnlyCollection<ClassRoomDto>? classes;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private void NavigateToDetail(string classId) => Navigation.NavigateTo($"/admin/classes/{classId}");

    private async Task LoadAsync() =>
        await ExecuteAsync(async () => classes = await Api.GetClassesAsync(), "Không tải được danh sách lớp");

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<ClassFormDialog> { { x => x.Model, new CreateClassRoomRequest("") } };
        var data = await ShowFormDialogAsync<ClassFormDialog, CreateClassRoomRequest>("Thêm lớp", parameters);
        if (data == null)
            return;

        await ExecuteAsync(() => Api.CreateClassAsync(data), "Tạo thất bại", "Đã thêm lớp.");
        await LoadAsync();
    }

    private Task DeleteAsync(ClassRoomDto classRoom) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá lớp '{classRoom.Name}'? Học viên đã trong lớp sẽ mất quyền truy cập các đề thi gán riêng cho lớp này.",
        async () =>
        {
            await Api.DeleteClassAsync(classRoom.Id);
            await LoadAsync();
        },
        "Xoá thất bại", "Đã xoá.");
}
