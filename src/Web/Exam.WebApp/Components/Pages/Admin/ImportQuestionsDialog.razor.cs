using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Exam.WebApp.Components.Pages.Admin;

public partial class ImportQuestionsDialog : FormDialogBase
{
    private IBrowserFile? selectedFile;

    private void Submit()
    {
        if (selectedFile == null)
            return;

        MudDialog.Close(DialogResult.Ok(selectedFile));
    }
}
