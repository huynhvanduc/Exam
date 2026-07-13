using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IExamRepository _examRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public GetDashboardSummaryQueryHandler(ICategoryRepository categoryRepository, IQuestionRepository questionRepository,
        IExamRepository examRepository, IUserRepository userRepository, IClassRoomRepository classRoomRepository,
        IAuditLogRepository auditLogRepository)
    {
        _categoryRepository = categoryRepository;
        _questionRepository = questionRepository;
        _examRepository = examRepository;
        _userRepository = userRepository;
        _classRoomRepository = classRoomRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var categoriesTask = _categoryRepository.GetAllAsync(cancellationToken);
        var questionCountTask = _questionRepository.CountAsync(cancellationToken);
        var examCountTask = _examRepository.CountAsync(cancellationToken);
        var publishedExamCountTask = _examRepository.CountByStatusAsync(ExamStatus.Published, cancellationToken);
        var userCountTask = _userRepository.CountAsync(cancellationToken);
        var classesTask = _classRoomRepository.GetAllAsync(cancellationToken);
        var recentActivityTask = _auditLogRepository.GetPagedAsync(0, 5, cancellationToken);

        await Task.WhenAll(categoriesTask, questionCountTask, examCountTask, publishedExamCountTask, userCountTask,
            classesTask, recentActivityTask);

        var recentActivity = (await recentActivityTask)
            .Select(e => new AuditLogEntryDto(e.Id, e.Timestamp, e.ActorUserId, e.Action, e.TargetId, e.Description))
            .ToList();

        return new DashboardSummaryDto(
            (await categoriesTask).Count,
            (int)await questionCountTask,
            (int)await examCountTask,
            (int)await publishedExamCountTask,
            (int)await userCountTask,
            (await classesTask).Count,
            recentActivity);
    }
}
