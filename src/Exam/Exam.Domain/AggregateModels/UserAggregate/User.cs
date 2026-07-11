using Exam.Domain.Enums;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.UserAggregate;

public class User : Entity, IAggregateRoot
{
    public string ExternalId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public UserRole Role { get; private set; }

    private User()
    {
    }

    public User(string externalId, string firstName, string lastName, UserRole role = UserRole.Student)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ExamDomainException("User external id is required.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ExamDomainException("User first name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ExamDomainException("User last name is required.");

        ExternalId = externalId;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public static User CreateNewUser(string externalId, string firstName, string lastName, UserRole role = UserRole.Student) =>
        new(externalId, firstName, lastName, role);

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ExamDomainException("User first name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ExamDomainException("User last name is required.");

        FirstName = firstName;
        LastName = lastName;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }
}
