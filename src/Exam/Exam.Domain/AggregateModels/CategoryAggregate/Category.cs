using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.CategoryAggregate;

public class Category : Entity, IAggregateRoot
{
    public string Name { get; private set; }

    public string UrlPath { get; private set; }

    private Category()
    {
    }

    public static Category Create(string name, string urlPath)
    {
        var category = new Category();
        category.SetName(name);
        category.SetUrlPath(urlPath);
        return category;
    }

    public void Rename(string name) => SetName(name);

    public void ChangeUrlPath(string urlPath) => SetUrlPath(urlPath);

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Category name is required.");

        Name = name;
    }

    private void SetUrlPath(string urlPath)
    {
        if (string.IsNullOrWhiteSpace(urlPath))
            throw new ExamDomainException("Category url path is required.");

        UrlPath = urlPath;
    }
}
