namespace Exam.WebApp.Components.Pages;

public partial class MyClasses : PageBase
{
    private IReadOnlyCollection<ClassRoomDto>? classes;
    private string joinCode = "";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private Task LoadAsync() => ExecuteAsync(async () => classes = await Api.GetMyClassesAsync(), "Không tải được danh sách lớp");

    private Task JoinAsync() => ExecuteAsync(async () =>
    {
        await Api.JoinClassAsync(joinCode);
        joinCode = "";
        await LoadAsync();
    }, "Không tham gia được lớp (kiểm tra lại mã lớp)", "Đã tham gia lớp.");
}
