using Exam.Contracts;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.UserAggregate;

public class UserTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("User_Constructor.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string externalId, string firstName, string lastName, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new User(externalId, "user@example.com", firstName, lastName));
            return;
        }

        var user = new User(externalId, "user@example.com", firstName, lastName);

        Assert.Equal(externalId, user.ExternalId);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(UserRole.Student, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Constructor_NullEmail_BecomesEmptyString()
    {
        var user = new User("ext-1", null!, "Nguyễn", "Văn A");

        Assert.Equal(string.Empty, user.Email);
    }

    [Fact]
    public void CreateNewUser_DelegatesToConstructor()
    {
        var user = User.CreateNewUser("ext-1", "user@example.com", "Nguyễn", "Văn A", UserRole.Instructor);

        Assert.Equal("ext-1", user.ExternalId);
        Assert.Equal(UserRole.Instructor, user.Role);
    }

    public static IEnumerable<object?[]> UpdateProfileCases() => CsvTestData.Read("User_UpdateProfile.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(UpdateProfileCases))]
    public void UpdateProfile(string firstName, string lastName, bool shouldThrow)
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => user.UpdateProfile("new@example.com", firstName, lastName));
            return;
        }

        user.UpdateProfile("new@example.com", firstName, lastName);

        Assert.Equal("new@example.com", user.Email);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
    }

    [Fact]
    public void UpdateProfile_NullEmail_BecomesEmptyString()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");

        user.UpdateProfile(null!, "Trần", "Thị B");

        Assert.Equal(string.Empty, user.Email);
    }

    [Fact]
    public void ChangeRole_UpdatesRole()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");

        user.ChangeRole(UserRole.Admin);

        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var user = new User("ext-1", "user@example.com", "Nguyễn", "Văn A");
        user.Deactivate();

        user.Activate();

        Assert.True(user.IsActive);
    }
}
