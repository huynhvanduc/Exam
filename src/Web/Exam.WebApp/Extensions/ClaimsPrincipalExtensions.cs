using System.Security.Claims;

namespace Exam.WebApp.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Nơi điều hướng về khi gặp lỗi 404/403 - tuỳ theo vai trò để tránh đưa user vào một trang
    // họ cũng không có quyền xem (vd: đưa Student về /admin/dashboard sẽ chỉ tạo thêm một lỗi 403 khác).
    public static string GetDefaultLandingPath(this ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return "/account/login";

        if (user.IsInRole("Admin"))
            return "/admin/dashboard";

        if (user.IsInRole("Instructor"))
            return "/admin/categories";

        return "/exams";
    }
}
