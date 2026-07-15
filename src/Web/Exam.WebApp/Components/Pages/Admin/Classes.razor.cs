using Exam.WebApp.Components.UI;
using Exam.WebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class Classes : AdminPageBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public string? Id { get; set; }

    private IReadOnlyCollection<ClassRoomDto>? classes;
    private UserDto? currentUser;
    private ClassRoomDetailDto? selected;
    private string renameValue = "";

    protected override async Task OnInitializedAsync() => await ExecuteAsync(async () =>
    {
        currentUser = await Api.GetMeAsync();
        classes = await Api.GetClassesAsync();
    }, "Không tải được danh sách lớp");

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(Id))
        {
            selected = null;
            return;
        }

        if (selected?.Id == Id)
            return;

        await ExecuteAsync(async () =>
        {
            selected = await Api.GetClassByIdAsync(Id);
            renameValue = selected.Name;
        }, "Không tải được lớp học");
    }

    private Task LoadListAsync() =>
        ExecuteAsync(async () => classes = await Api.GetClassesAsync(), "Không tải được danh sách lớp");

    private void SelectClass(string id) => Navigation.NavigateTo($"/admin/classes/{id}");

    private async Task OpenCreateDialog()
    {
        var parameters = new Dictionary<string, object> { ["Model"] = new CreateClassRoomRequest("") };
        var data = await ShowFormDialogAsync<ClassFormDialog, CreateClassRoomRequest>("Thêm lớp", parameters);
        if (data == null)
            return;

        ClassRoomDto? created = null;
        await ExecuteAsync(async () => created = await Api.CreateClassAsync(data), "Tạo thất bại", "Đã thêm lớp.");
        await LoadListAsync();
        if (created != null)
            SelectClass(created.Id);
    }

    private Task DeleteAsync(ClassRoomDto classRoom) => ConfirmAndExecuteAsync(
        "Xác nhận xoá", $"Xoá lớp '{classRoom.Name}'? Học viên đã trong lớp sẽ mất quyền truy cập các đề thi gán riêng cho lớp này.",
        async () =>
        {
            await Api.DeleteClassAsync(classRoom.Id);
            if (selected?.Id == classRoom.Id)
            {
                selected = null;
                Navigation.NavigateTo("/admin/classes");
            }
            await LoadListAsync();
        },
        "Xoá thất bại", "Đã xoá.");

    private Task RenameAsync() => ExecuteAsync(async () =>
    {
        await Api.RenameClassAsync(selected!.Id, new RenameClassRoomRequest(renameValue));
        selected = await Api.GetClassByIdAsync(selected.Id);
        await LoadListAsync();
    }, "Đổi tên thất bại", "Đã lưu.");

    private Task RegenerateCodeAsync() => ConfirmAndExecuteAsync(
        "Xác nhận tạo mã mới", "Tạo mã lớp mới? Mã cũ sẽ không còn dùng được để tham gia lớp.",
        async () =>
        {
            await Api.RegenerateJoinCodeAsync(selected!.Id);
            selected = await Api.GetClassByIdAsync(selected.Id);
        },
        "Tạo mã mới thất bại", "Đã tạo mã mới.", yesText: "Tạo mã mới");

    private Task RemoveMemberAsync(ClassMemberDto member) => ConfirmAndExecuteAsync(
        "Xác nhận gỡ thành viên", $"Gỡ '{member.FullName}' khỏi lớp?",
        async () =>
        {
            selected = await Api.RemoveClassMemberAsync(selected!.Id, member.UserId);
            await LoadListAsync();
        },
        "Gỡ thất bại", "Đã gỡ.", yesText: "Gỡ");

    private async Task OpenImportMembersDialogAsync()
    {
        // Dialog tự lo bước Xem trước (dry-run) + Xác nhận nhập bên trong nó, chỉ đóng lại và trả về
        // kết quả cuối cùng SAU KHI admin đã xác nhận (Cancel nếu admin bỏ ngang ở bất kỳ bước nào).
        var parameters = new Dictionary<string, object> { ["ClassId"] = selected!.Id };
        var result = await ShowFormDialogAsync<ImportClassMembersDialog, ImportClassMembersResultDto>("Nhập danh sách học viên từ Excel", parameters);
        if (result == null)
            return;

        var message = result.AddedCount > 0
            ? $"Đã thêm {result.AddedCount} học viên, {result.AlreadyMemberCount} đã là thành viên sẵn, {result.Errors.Count} lỗi."
            : $"Không có học viên nào được thêm ({result.TotalRows} dòng, {result.AlreadyMemberCount} đã là thành viên, {result.Errors.Count} lỗi).";

        Toast.Add(message, result.Errors.Count == 0 ? AppSeverity.Success : AppSeverity.Warning);

        selected = await Api.GetClassByIdAsync(selected.Id);
        await LoadListAsync();
    }

    private Task ExportMembersAsync() => ExecuteAsync(async () =>
    {
        var bytes = await Api.ExportClassMembersAsync(selected!.Id);
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", $"thanh-vien-lop-{selected.Id}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64);
    }, "Xuất Excel thất bại");
}
