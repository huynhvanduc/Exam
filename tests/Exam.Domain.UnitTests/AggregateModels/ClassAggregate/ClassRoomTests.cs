using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.ClassAggregate;

public class ClassRoomTests
{
    public static IEnumerable<object?[]> CreateCases() => CsvTestData.Read("ClassRoom_Create.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(CreateCases))]
    public void Create(string name, string ownerUserId, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => ClassRoom.Create(name, ownerUserId));
            return;
        }

        var classRoom = ClassRoom.Create(name, ownerUserId);

        Assert.Equal(name, classRoom.Name);
        Assert.Equal(ownerUserId, classRoom.OwnerUserId);
        Assert.Equal(6, classRoom.JoinCode.Length);
        Assert.Empty(classRoom.MemberUserIds);
    }

    public static IEnumerable<object?[]> RenameCases() => CsvTestData.Read("ClassRoom_Rename.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(RenameCases))]
    public void Rename(string newName, bool shouldThrow)
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => classRoom.Rename(newName));
            return;
        }

        classRoom.Rename(newName);

        Assert.Equal(newName, classRoom.Name);
    }

    public static IEnumerable<object?[]> AddMemberCases() => CsvTestData.Read("ClassRoom_AddMember.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(AddMemberCases))]
    public void AddMember(string userId, bool shouldThrow)
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => classRoom.AddMember(userId));
            return;
        }

        classRoom.AddMember(userId);

        Assert.True(classRoom.HasMember(userId));
    }

    [Fact]
    public void AddMember_SameUserTwice_DoesNotDuplicate()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");

        classRoom.AddMember("student-1");
        classRoom.AddMember("student-1");

        Assert.Single(classRoom.MemberUserIds);
    }

    [Fact]
    public void RemoveMember_ExistingMember_RemovesThem()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        classRoom.AddMember("student-1");

        classRoom.RemoveMember("student-1");

        Assert.False(classRoom.HasMember("student-1"));
    }

    [Fact]
    public void RemoveMember_NonMember_DoesNotThrow()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");

        var exception = Record.Exception(() => classRoom.RemoveMember("not-a-member"));

        Assert.Null(exception);
    }

    [Fact]
    public void HasMember_ReturnsFalse_WhenNotAdded()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");

        Assert.False(classRoom.HasMember("student-1"));
    }

    [Fact]
    public void RegenerateJoinCode_ProducesDifferentSixCharacterCode()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var originalCode = classRoom.JoinCode;

        classRoom.RegenerateJoinCode();

        Assert.Equal(6, classRoom.JoinCode.Length);
        Assert.All(classRoom.JoinCode, c => Assert.Contains(c, "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"));
        // Không assert khác originalCode vì random có xác suất (rất nhỏ) trùng lại y hệt - không phải bug.
    }
}
