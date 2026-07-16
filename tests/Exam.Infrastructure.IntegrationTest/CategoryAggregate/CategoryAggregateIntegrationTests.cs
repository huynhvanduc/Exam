using Exam.Application.CategoryAggregate.Commands.CreateCategory;
using Exam.Application.CategoryAggregate.Commands.DeleteCategory;
using Exam.Application.CategoryAggregate.Commands.UpdateCategory;
using Exam.Application.CategoryAggregate.Queries.GetCategories;
using Exam.Application.CategoryAggregate.Queries.GetCategoryById;
using Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;
using FluentValidation;

namespace Exam.Infrastructure.IntegrationTest.CategoryAggregate;

public class CategoryAggregateIntegrationTests
{
    private readonly IntegrationTestFixture _fixture = new();

    [Fact]
    public async Task CreateCategory_ValidCommand_InsertsCategoryAndWritesAuditLog()
    {
        _fixture.CurrentUser.UserId = "admin-1";

        var result = await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));

        Assert.Equal("Toán học", result.Name);
        Assert.Single(_fixture.CategoryRepository.Items);

        var auditEntry = Assert.Single(_fixture.AuditLogRepository.Items);
        Assert.Equal("admin-1", auditEntry.ActorUserId);
        Assert.Equal("Category.Create", auditEntry.Action);
        Assert.Contains("toan-hoc", auditEntry.Description);
    }

    [Fact]
    public async Task CreateCategory_InvalidCommand_ThrowsValidationExceptionAndWritesNoData()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _fixture.Mediator.Send(new CreateCategoryCommand("", "toan-hoc")));

        Assert.Empty(_fixture.CategoryRepository.Items);
        Assert.Empty(_fixture.AuditLogRepository.Items);
    }

    [Fact]
    public async Task UpdateCategory_ValidCommand_UpdatesCategoryAndWritesAuditLog()
    {
        _fixture.CurrentUser.UserId = "admin-1";
        var created = await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));

        var updated = await _fixture.Mediator.Send(new UpdateCategoryCommand(created.Id, "Toán học nâng cao", "toan-hoc-nang-cao"));

        Assert.Equal("Toán học nâng cao", updated.Name);
        Assert.Equal("toan-hoc-nang-cao", _fixture.CategoryRepository.Items.Single().UrlPath);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Category.Update");
    }

    [Fact]
    public async Task DeleteCategory_NoExamsOrQuestions_DeletesCategoryAndWritesAuditLog()
    {
        _fixture.CurrentUser.UserId = "admin-1";
        var created = await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));

        await _fixture.Mediator.Send(new DeleteCategoryCommand(created.Id));

        Assert.Empty(_fixture.CategoryRepository.Items);
        Assert.Contains(_fixture.AuditLogRepository.Items, e => e.Action == "Category.Delete");
    }

    [Fact]
    public async Task DeleteCategory_CategoryHasExam_ThrowsDomainExceptionAndDoesNotDelete()
    {
        var created = await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, "teacher-1", created.Id, created.Name, true, 5m);
        await _fixture.ExamRepository.InsertAsync(exam);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            _fixture.Mediator.Send(new DeleteCategoryCommand(created.Id)));

        Assert.Single(_fixture.CategoryRepository.Items);
    }

    [Fact]
    public async Task GetCategories_ReturnsAllInsertedCategories()
    {
        await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));
        await _fixture.Mediator.Send(new CreateCategoryCommand("Vật lý", "vat-ly"));

        var result = await _fixture.Mediator.Send(new GetCategoriesQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetCategoryById_ReturnsMatchingCategory()
    {
        var created = await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));

        var result = await _fixture.Mediator.Send(new GetCategoryByIdQuery(created.Id));

        Assert.NotNull(result);
        Assert.Equal("Toán học", result!.Name);
    }

    [Fact]
    public async Task GetCategoryByUrlPath_ReturnsMatchingCategory()
    {
        await _fixture.Mediator.Send(new CreateCategoryCommand("Toán học", "toan-hoc"));

        var result = await _fixture.Mediator.Send(new GetCategoryByUrlPathQuery("toan-hoc"));

        Assert.NotNull(result);
        Assert.Equal("toan-hoc", result!.UrlPath);
    }
}
