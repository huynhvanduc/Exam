using Exam.Contracts;
using Exam.Domain.Exceptions;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;
using ExamCompositionCell = Exam.Domain.AggregateModels.ExamAggregate.ExamCompositionCell;

namespace Exam.Domain.UnitTests.AggregateModels.ExamAggregate;

public class ExamTests
{
    private static readonly DateTime Anchor = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ExamEntity CreateValidExam() =>
        new("Đề kiểm tra", "Mô tả ngắn", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy, "teacher-1",
            "cat-1", "Toán học", true, 5m);

    public static IEnumerable<object?[]> ConstructorCases() => CsvTestData.Read("Exam_Constructor.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Decimal, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConstructorCases))]
    public void Constructor(string name, string categoryId, decimal minimumPassingScore, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() =>
                new ExamEntity(name, "desc", "content", TimeSpan.FromMinutes(30), Level.Easy, "teacher-1",
                    categoryId, "Category", true, minimumPassingScore));
            return;
        }

        var exam = new ExamEntity(name, "desc", "content", TimeSpan.FromMinutes(30), Level.Easy, "teacher-1",
            categoryId, "Category", true, minimumPassingScore);

        Assert.Equal(name, exam.Name);
        Assert.Equal(categoryId, exam.CategoryId);
        Assert.Equal(minimumPassingScore, exam.MinimumPassingScore);
        Assert.Equal(ExamStatus.Draft, exam.Status);
        Assert.True(exam.IsPublic);
        Assert.Equal(0, exam.NumberOfQuestions);
    }

    public static IEnumerable<object?[]> UpdateDetailsCases() => CsvTestData.Read("Exam_UpdateDetails.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Decimal, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(UpdateDetailsCases))]
    public void UpdateDetails_WhenDraft(string name, string categoryId, decimal minimumPassingScore, bool shouldThrow)
    {
        var exam = CreateValidExam();

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() =>
                exam.UpdateDetails(name, "desc", "content", TimeSpan.FromMinutes(45), Level.Medium,
                    categoryId, "Category mới", false, minimumPassingScore));
            return;
        }

        exam.UpdateDetails(name, "desc", "content", TimeSpan.FromMinutes(45), Level.Medium,
            categoryId, "Category mới", false, minimumPassingScore);

        Assert.Equal(name, exam.Name);
        Assert.Equal(categoryId, exam.CategoryId);
        Assert.Equal(minimumPassingScore, exam.MinimumPassingScore);
    }

    [Fact]
    public void UpdateDetails_WhenNotDraft_Throws()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();

        Assert.Throws<ExamDomainException>(() =>
            exam.UpdateDetails("Tên mới", "desc", "content", TimeSpan.FromMinutes(30), Level.Easy,
                "cat-1", "Category", true, 5m));
    }

    public static IEnumerable<object?[]> ConfigureCompositionCases() => CsvTestData.Read("Exam_ConfigureComposition.csv",
        CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConfigureCompositionCases))]
    public void ConfigureComposition_SingleCell(int cellCount, bool shouldThrow)
    {
        var exam = CreateValidExam();
        var cells = new[] { new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, cellCount) };

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => exam.ConfigureComposition(cells));
            return;
        }

        exam.ConfigureComposition(cells);

        Assert.Equal(cellCount, exam.NumberOfQuestions);
    }

    [Fact]
    public void ConfigureComposition_NullCells_Throws()
    {
        var exam = CreateValidExam();

        Assert.Throws<ExamDomainException>(() => exam.ConfigureComposition(null!));
    }

    [Fact]
    public void ConfigureComposition_EmptyCells_Throws()
    {
        var exam = CreateValidExam();

        Assert.Throws<ExamDomainException>(() => exam.ConfigureComposition([]));
    }

    [Fact]
    public void ConfigureComposition_WhenNotDraft_Throws()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();

        Assert.Throws<ExamDomainException>(() =>
            exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 3)]));
    }

    public static IEnumerable<object?[]> ScheduleAvailabilityCases() => CsvTestData.Read("Exam_ScheduleAvailability.csv",
        CsvTestData.NullableInt, CsvTestData.NullableInt, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ScheduleAvailabilityCases))]
    public void ScheduleAvailability(int? fromOffsetDays, int? toOffsetDays, bool shouldThrow)
    {
        var exam = CreateValidExam();
        var from = fromOffsetDays.HasValue ? Anchor.AddDays(fromOffsetDays.Value) : (DateTime?)null;
        var to = toOffsetDays.HasValue ? Anchor.AddDays(toOffsetDays.Value) : (DateTime?)null;

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => exam.ScheduleAvailability(from, to));
            return;
        }

        exam.ScheduleAvailability(from, to);

        Assert.Equal(from, exam.AvailableFrom);
        Assert.Equal(to, exam.AvailableTo);
    }

    [Fact]
    public void ScheduleAvailability_WhenArchived_Throws()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Archive();

        Assert.Throws<ExamDomainException>(() => exam.ScheduleAvailability(null, null));
    }

    public static IEnumerable<object?[]> IsAvailableCases() => CsvTestData.Read("Exam_IsAvailable.csv",
        CsvTestData.NullableInt, CsvTestData.NullableInt, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(IsAvailableCases))]
    public void IsAvailable_WhenPublished(int? fromOffsetDays, int? toOffsetDays, int atOffsetDays, bool expectedAvailable)
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();
        var from = fromOffsetDays.HasValue ? Anchor.AddDays(fromOffsetDays.Value) : (DateTime?)null;
        var to = toOffsetDays.HasValue ? Anchor.AddDays(toOffsetDays.Value) : (DateTime?)null;
        exam.ScheduleAvailability(from, to);

        var result = exam.IsAvailable(Anchor.AddDays(atOffsetDays));

        Assert.Equal(expectedAvailable, result);
    }

    [Fact]
    public void IsAvailable_WhenDraft_ReturnsFalse()
    {
        var exam = CreateValidExam();

        Assert.False(exam.IsAvailable(Anchor));
    }

    public static IEnumerable<object?[]> ConfigureMaxAttemptsCases() => CsvTestData.Read("Exam_ConfigureMaxAttempts.csv",
        CsvTestData.NullableInt, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ConfigureMaxAttemptsCases))]
    public void ConfigureMaxAttempts(int? maxAttempts, bool shouldThrow)
    {
        var exam = CreateValidExam();

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => exam.ConfigureMaxAttempts(maxAttempts));
            return;
        }

        exam.ConfigureMaxAttempts(maxAttempts);

        Assert.Equal(maxAttempts, exam.MaxAttempts);
    }

    [Fact]
    public void Publish_WhenDraftWithQuestions_Succeeds()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);

        exam.Publish();

        Assert.Equal(ExamStatus.Published, exam.Status);
    }

    [Fact]
    public void Publish_WhenNoQuestions_Throws()
    {
        var exam = CreateValidExam();

        Assert.Throws<ExamDomainException>(() => exam.Publish());
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_Throws()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();

        Assert.Throws<ExamDomainException>(() => exam.Publish());
    }

    [Fact]
    public void Publish_WhenArchived_Throws()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Archive();

        Assert.Throws<ExamDomainException>(() => exam.Publish());
    }

    [Fact]
    public void Unpublish_WhenPublished_Succeeds()
    {
        var exam = CreateValidExam();
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();

        exam.Unpublish();

        Assert.Equal(ExamStatus.Draft, exam.Status);
    }

    [Fact]
    public void Unpublish_WhenDraft_Throws()
    {
        var exam = CreateValidExam();

        Assert.Throws<ExamDomainException>(() => exam.Unpublish());
    }

    [Fact]
    public void Archive_WhenDraft_Succeeds()
    {
        var exam = CreateValidExam();

        exam.Archive();

        Assert.Equal(ExamStatus.Archived, exam.Status);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_Throws()
    {
        var exam = CreateValidExam();
        exam.Archive();

        Assert.Throws<ExamDomainException>(() => exam.Archive());
    }

    public static IEnumerable<object?[]> AssignToClassCases() => CsvTestData.Read("Exam_AssignToClass.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(AssignToClassCases))]
    public void AssignToClass(string classId, bool shouldThrow)
    {
        var exam = CreateValidExam();

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => exam.AssignToClass(classId));
            return;
        }

        exam.AssignToClass(classId);

        Assert.Contains(classId, exam.AssignedClassIds);
        Assert.False(exam.IsPublic);
    }

    [Fact]
    public void AssignToClass_SameClassTwice_DoesNotDuplicate()
    {
        var exam = CreateValidExam();

        exam.AssignToClass("class-1");
        exam.AssignToClass("class-1");

        Assert.Single(exam.AssignedClassIds);
    }

    [Fact]
    public void UnassignFromClass_RemovesAssignedClass()
    {
        var exam = CreateValidExam();
        exam.AssignToClass("class-1");

        exam.UnassignFromClass("class-1");

        Assert.DoesNotContain("class-1", exam.AssignedClassIds);
        Assert.True(exam.IsPublic);
    }

    [Fact]
    public void UnassignFromClass_NotAssigned_DoesNotThrow()
    {
        var exam = CreateValidExam();

        var exception = Record.Exception(() => exam.UnassignFromClass("class-1"));

        Assert.Null(exception);
    }

    [Fact]
    public void IsAssignedToAnyOf_PublicExam_AlwaysTrue()
    {
        var exam = CreateValidExam();

        Assert.True(exam.IsAssignedToAnyOf(["some-class"]));
        Assert.True(exam.IsAssignedToAnyOf([]));
    }

    [Fact]
    public void IsAssignedToAnyOf_PrivateExam_MatchingClass_ReturnsTrue()
    {
        var exam = CreateValidExam();
        exam.AssignToClass("class-1");

        Assert.True(exam.IsAssignedToAnyOf(["class-1", "class-2"]));
    }

    [Fact]
    public void IsAssignedToAnyOf_PrivateExam_NonMatchingClass_ReturnsFalse()
    {
        var exam = CreateValidExam();
        exam.AssignToClass("class-1");

        Assert.False(exam.IsAssignedToAnyOf(["class-2", "class-3"]));
    }
}
