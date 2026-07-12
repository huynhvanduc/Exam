using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Components.Pages;

public partial class Home : ComponentBase
{
    private void GoToLogin() => Navigation.NavigateTo("/account/login", forceLoad: true);
}
