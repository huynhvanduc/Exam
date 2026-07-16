using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.UnitTests.AggregateModels.CategoryAggregate;

public class CategoryTests
{
    public static IEnumerable<object?[]> CreateCases() => CsvTestData.Read("Category_Create.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(CreateCases))]
    public void Create(string name, string urlPath, bool shouldThrow)
    {
        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => Category.Create(name, urlPath));
            return;
        }

        var category = Category.Create(name, urlPath);

        Assert.Equal(name, category.Name);
        Assert.Equal(urlPath, category.UrlPath);
    }

    public static IEnumerable<object?[]> RenameCases() => CsvTestData.Read("Category_Rename.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(RenameCases))]
    public void Rename(string newName, bool shouldThrow)
    {
        var category = Category.Create("Toán học", "toan-hoc");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => category.Rename(newName));
            return;
        }

        category.Rename(newName);

        Assert.Equal(newName, category.Name);
    }

    public static IEnumerable<object?[]> ChangeUrlPathCases() => CsvTestData.Read("Category_ChangeUrlPath.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(ChangeUrlPathCases))]
    public void ChangeUrlPath(string newUrlPath, bool shouldThrow)
    {
        var category = Category.Create("Toán học", "toan-hoc");

        if (shouldThrow)
        {
            Assert.Throws<ExamDomainException>(() => category.ChangeUrlPath(newUrlPath));
            return;
        }

        category.ChangeUrlPath(newUrlPath);

        Assert.Equal(newUrlPath, category.UrlPath);
    }
}
