using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.UserAggregate;

public class User : Entity, IAggregateRoot
{
    public string ExternalId { get; private set; }

    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public UserRole Role { get; private set; }

    // Không đưa vào constructor có tham số (MapCreator ở MongoClassMaps) để tránh vỡ deserialize
    // các document cũ chưa có field này - default true áp dụng luôn cho cả document cũ lẫn user mới.
    public bool IsActive { get; private set; } = true;

    private User()
    {
    }

    public User(string externalId, string email, string firstName, string lastName, UserRole role = UserRole.Student)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ExamDomainException("User external id is required.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ExamDomainException("User first name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ExamDomainException("User last name is required.");

        ExternalId = externalId;
        Email = email ?? string.Empty;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public static User CreateNewUser(string externalId, string email, string firstName, string lastName, UserRole role = UserRole.Student) =>
        new(externalId, email, firstName, lastName, role);

    public void UpdateProfile(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ExamDomainException("User first name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ExamDomainException("User last name is required.");

        Email = email ?? string.Empty;
        FirstName = firstName;
        LastName = lastName;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
