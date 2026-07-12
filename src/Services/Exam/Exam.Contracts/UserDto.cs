namespace Exam.Contracts;

public record UserDto(string Id, string ExternalId, string FirstName, string LastName, UserRole Role);
