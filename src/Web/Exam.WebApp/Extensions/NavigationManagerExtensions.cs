using Microsoft.AspNetCore.Components;

namespace Exam.WebApp.Extensions;

public static class NavigationManagerExtensions
{
    public static void GoToLogin(this NavigationManager navigation) =>
        navigation.NavigateTo("/account/login", forceLoad: true);
}
