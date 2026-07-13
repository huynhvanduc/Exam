namespace Exam.Contracts;

public record UserDto(string Id, string ExternalId, string Email, string FirstName, string LastName, UserRole Role, bool IsActive);
