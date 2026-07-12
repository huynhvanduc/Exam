using Exam.Domain.Enums;

namespace Exam.Application.UserAggregate;

public record UserDto(string Id, string ExternalId, string FirstName, string LastName, UserRole Role);
