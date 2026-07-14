using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.MoveQuestions;

public class MoveQuestionsCommandHandler : IRequestHandler<MoveQuestionsCommand>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public MoveQuestionsCommandHandler(IQuestionRepository questionRepository, ICategoryRepository categoryRepository)
    {
        _questionRepository = questionRepository;
        _categoryRepository = categoryRepository;
    }

    // Tất cả hoặc không gì cả: kiểm tra môn học đích + toàn bộ câu hỏi (tồn tại + quyền sở hữu) TRƯỚC,
    // chỉ ghi (UpdateAsync) SAU KHI mọi câu hỏi đã qua hết bước kiểm tra - tránh tình trạng chuyển được
    // một phần rồi mới báo lỗi ở giữa chừng khi có 1 câu hỏi không hợp lệ trong danh sách đã chọn.
    public async Task Handle(MoveQuestionsCommand request, CancellationToken cancellationToken)
    {
        var targetCategory = await _categoryRepository.GetByIdAsync(request.TargetCategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.TargetCategoryId);

        var questions = await _questionRepository.GetByIdsAsync(request.QuestionIds, cancellationToken);
        if (questions.Count != request.QuestionIds.Count)
        {
            var foundIds = questions.Select(q => q.Id).ToHashSet();
            var missingId = request.QuestionIds.First(id => !foundIds.Contains(id));
            throw NotFoundException.For(nameof(Question), missingId);
        }

        foreach (var question in questions)
            OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, question.OwnerUserId, nameof(Question), question.Id);

        foreach (var question in questions)
        {
            question.ChangeCategory(targetCategory.Id, targetCategory.Name);
            await _questionRepository.UpdateAsync(question, cancellationToken);
        }
    }
}
