using Exam.Application;
using Exam.Application.Common;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;
using Exam.Infrastructure.IntegrationTest.Fakes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Exam.Infrastructure.IntegrationTest;

// Dựng lại đúng pipeline MediatR thật (ValidationBehavior + AuditLoggingBehavior) như production,
// chỉ thay tầng persistence bằng fake repository in-memory - khác Exam.Application.UnitTests (mock
// từng lời gọi repository riêng lẻ trên Handler trần), test ở đây gọi qua IMediator.Send(...) nên bắt
// được lỗi wiring/pipeline mà unit test mock không thấy (validator có thật sự chặn input sai không,
// audit log có thật sự được ghi đúng nội dung không...).
// Mỗi test class instance mới (xUnit tạo instance riêng cho từng [Fact]) tương ứng 1 fixture mới -
// state in-memory không rò rỉ giữa các test.
public class IntegrationTestFixture
{
    public TestCurrentUserAccessor CurrentUser { get; } = new();

    public FakeCategoryRepository CategoryRepository { get; } = new();

    public FakeExamRepository ExamRepository { get; } = new();

    public FakeQuestionRepository QuestionRepository { get; } = new();

    public FakeAuditLogRepository AuditLogRepository { get; } = new();

    public IServiceProvider Services { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        services.AddLogging();

        services.AddSingleton<ICurrentUserAccessor>(CurrentUser);
        services.AddSingleton<ICategoryRepository>(CategoryRepository);
        services.AddSingleton<IExamRepository>(ExamRepository);
        services.AddSingleton<IQuestionRepository>(QuestionRepository);
        services.AddSingleton<IAuditLogRepository>(AuditLogRepository);

        services.AddScoped<CategoryDeletionGuard>();
        services.AddScoped<ExamQuestionPoolService>();

        Services = services.BuildServiceProvider();
    }

    public IMediator Mediator => Services.GetRequiredService<IMediator>();
}
