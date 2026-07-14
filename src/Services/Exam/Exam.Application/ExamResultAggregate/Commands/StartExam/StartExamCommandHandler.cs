using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.StartExam;

public class StartExamCommandHandler : IRequestHandler<StartExamCommand, ExamAttemptDto>
{
    private readonly IExamRepository _examRepository;
    private readonly IExamResultRepository _examResultRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassRoomRepository _classRoomRepository;
    private readonly ExamQuestionPoolService _examQuestionPoolService;

    public StartExamCommandHandler(IExamRepository examRepository, IExamResultRepository examResultRepository,
        IQuestionRepository questionRepository, IUserRepository userRepository, IClassRoomRepository classRoomRepository,
        ExamQuestionPoolService examQuestionPoolService)
    {
        _examRepository = examRepository;
        _examResultRepository = examResultRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _classRoomRepository = classRoomRepository;
        _examQuestionPoolService = examQuestionPoolService;
    }

    public async Task<ExamAttemptDto> Handle(StartExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        var existingAttempt = await _examResultRepository.GetInProgressAttemptAsync(request.UserId, request.ExamId, cancellationToken);
        if (existingAttempt != null)
        {
            var attemptQuestions = await _questionRepository.GetByIdsAsync(existingAttempt.QuestionIds, cancellationToken);
            return ExamResultMapper.ToAttemptDto(existingAttempt, attemptQuestions);
        }

        if (!exam.IsAvailable(DateTime.UtcNow))
            throw new ExamDomainException("This exam is not available right now.");

        if (exam.MaxAttempts.HasValue)
        {
            var attemptCount = await _examResultRepository.CountByUserIdAndExamIdAsync(request.UserId, request.ExamId, cancellationToken);
            if (attemptCount >= exam.MaxAttempts.Value)
                throw new ExamDomainException($"You have reached the maximum number of attempts ({exam.MaxAttempts.Value}) for this exam.");
        }

        // Chặn ở tầng command (không chỉ ẩn nút trên UI) - đề gán lớp chỉ thành viên lớp đó mới thi được,
        // tránh học viên gọi thẳng API để bypass giao diện.
        if (!exam.IsPublic)
        {
            var userClassIds = (await _classRoomRepository.GetByMemberAsync(request.UserId, cancellationToken))
                .Select(c => c.Id)
                .ToList();

            if (!exam.IsAssignedToAnyOf(userClassIds))
                throw new ForbiddenException("You are not assigned to take this exam.");
        }

        var questionIds = await _examQuestionPoolService.DrawQuestionIdsAsync(exam, cancellationToken);

        var user = await _userRepository.GetByExternalIdAsync(request.UserId, cancellationToken);
        var fullName = user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();

        var examResult = new ExamResult(request.UserId, request.ExamId);
        examResult.SetExamTitle(exam.Name);
        examResult.SetUserInfo(user?.Email ?? string.Empty, fullName);
        examResult.AssignQuestions(questionIds);
        examResult.SetDuration(exam.IsTimeRestricted ? exam.Duration : null);

        await _examResultRepository.InsertAsync(examResult, cancellationToken);

        var questions = await _questionRepository.GetByIdsAsync(questionIds, cancellationToken);

        return ExamResultMapper.ToAttemptDto(examResult, questions);
    }
}
