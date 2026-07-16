using Exam.Contracts;
using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.RoleAggregate;

public class RolePermissionSetTests
{
    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("RolePermissionSet_Constructor.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string roleStr, bool shouldThrow)
    {
        var role = Enum.Parse<UserRole>(roleStr);
        var permissions = new[] { "Category.Create", "Question.View" };

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => new RolePermissionSet(role, permissions));
            return;
        }

        var set = new RolePermissionSet(role, permissions);

        Assert.Equal(role, set.Role);
        Assert.Equal(2, set.Permissions.Count);
    }

    [Fact]
    public void Constructor_NullPermissions_ResultsInEmptySet()
    {
        var set = new RolePermissionSet(UserRole.Student, null!);

        Assert.Empty(set.Permissions);
    }

    [Fact]
    public void ReplacePermissions_ReplacesExistingSet()
    {
        var set = new RolePermissionSet(UserRole.Student, ["Category.Create"]);

        set.ReplacePermissions(["Question.View", "Question.Create"]);

        Assert.Equal(2, set.Permissions.Count);
        Assert.DoesNotContain("Category.Create", set.Permissions);
    }

    [Fact]
    public void ReplacePermissions_Null_ResultsInEmptySet()
    {
        var set = new RolePermissionSet(UserRole.Student, ["Category.Create"]);

        set.ReplacePermissions(null!);

        Assert.Empty(set.Permissions);
    }

    [Fact]
    public void Has_ExistingPermission_ReturnsTrue()
    {
        var set = new RolePermissionSet(UserRole.Student, ["Category.Create"]);

        Assert.True(set.Has("Category.Create"));
    }

    [Fact]
    public void Has_MissingPermission_ReturnsFalse()
    {
        var set = new RolePermissionSet(UserRole.Student, ["Category.Create"]);

        Assert.False(set.Has("Question.View"));
    }
}
