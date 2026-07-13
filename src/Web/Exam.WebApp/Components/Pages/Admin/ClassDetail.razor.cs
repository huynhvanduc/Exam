using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ClassDetail : AdminPageBase
{
    [Parameter]
    public string Id { get; set; } = "";

    private ClassRoomDetailDto? classRoom;
    private string name = "";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        classRoom = await Api.GetClassByIdAsync(Id);
        name = classRoom.Name;
    }, "Không tải được lớp học");

    private Task RenameAsync() => ExecuteAsync(async () =>
    {
        await Api.RenameClassAsync(Id, new RenameClassRoomRequest(name));
        await LoadAsync();
    }, "Đổi tên thất bại", "Đã lưu.");

    private Task RegenerateCodeAsync() => ExecuteAsync(async () =>
    {
        await Api.RegenerateJoinCodeAsync(Id);
        await LoadAsync();
    }, "Tạo mã mới thất bại", "Đã tạo mã mới.");

    private Task RemoveMemberAsync(ClassMemberDto member) => ConfirmAndExecuteAsync(
        "Xác nhận gỡ thành viên", $"Gỡ '{member.FullName}' khỏi lớp?",
        async () =>
        {
            classRoom = await Api.RemoveClassMemberAsync(Id, member.UserId);
        },
        "Gỡ thất bại", "Đã gỡ.", yesText: "Gỡ");
}
