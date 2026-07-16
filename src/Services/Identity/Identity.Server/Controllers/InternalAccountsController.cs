using System.Security.Cryptography;
using Identity.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Server.Controllers;

// Endpoint nội bộ, chỉ gọi server-to-server từ Exam.API (client_credentials, scope "identity_api.manage") -
// dùng để tạo tài khoản đăng nhập mới, vì Identity.Server là nơi duy nhất giữ mật khẩu (Exam.API chỉ lưu
// bản sao User ở Mongo, không có cơ chế tạo tài khoản đăng nhập).
[ApiController]
[Route("internal/accounts")]
[Authorize(AuthenticationSchemes = "Bearer", Policy = "IdentityApiManage")]
public class InternalAccountsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public InternalAccountsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public record CreateAccountRequest(string Email, string FirstName, string LastName);
    public record CreateAccountResponse(string ExternalId, string GeneratedPassword);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Conflict(new { error = "email_taken" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var password = GeneratePassword();
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new CreateAccountResponse(user.Id, password));
    }

    // Sinh mật khẩu ngẫu nhiên đáp ứng chính sách mật khẩu hiện tại (RequiredLength=8, RequireDigit=true,
    // RequireLowercase/RequireNonAlphanumeric mặc định true) - đảm bảo có đủ mỗi loại ký tự bắt buộc rồi
    // trộn ngẫu nhiên vị trí, dùng RandomNumberGenerator (crypto-secure) thay vì Random.
    private static string GeneratePassword()
    {
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@#$%?";
        const string all = lower + upper + digits + special;

        var chars = new List<char>
        {
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            special[RandomNumberGenerator.GetInt32(special.Length)]
        };

        for (var i = chars.Count; i < 12; i++)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
