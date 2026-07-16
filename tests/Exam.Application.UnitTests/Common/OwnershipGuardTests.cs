using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;

namespace Exam.Application.UnitTests.Common;

public class OwnershipGuardTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("OwnershipGuard_EnsureOwnerOrAdmin.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void EnsureOwnerOrAdmin(string actorRoleStr, string actorUserId, string ownerUserId, bool shouldThrow)
    {
        var actor = new Actor(actorUserId, Enum.Parse<UserRole>(actorRoleStr));

        if (shouldThrow)
        {
            Assert.Throws<ForbiddenException>(() => OwnershipGuard.EnsureOwnerOrAdmin(actor, ownerUserId, "ClassRoom", "class-1"));
            return;
        }

        var exception = Record.Exception(() => OwnershipGuard.EnsureOwnerOrAdmin(actor, ownerUserId, "ClassRoom", "class-1"));

        Assert.Null(exception);
    }
}
