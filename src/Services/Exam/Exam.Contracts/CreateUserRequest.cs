namespace Exam.Contracts;

public record CreateUserRequest(string Email, string FirstName, string LastName, UserRole Role);

public record CreateUserResponse(UserDto User, string GeneratedPassword)
{
    public string Id => User.Id;
}
