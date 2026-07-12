namespace Exam.WebApp.Components.Layout;

public partial class MainLayout
{
    private void GoToLogin() => Navigation.NavigateTo("/account/login", forceLoad: true);
}
