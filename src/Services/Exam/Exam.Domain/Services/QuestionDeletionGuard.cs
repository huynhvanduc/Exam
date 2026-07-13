using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Domain.Services;

public class QuestionDeletionGuard
{
    private readonly IExamRepository _examRepository;

    public QuestionDeletionGuard(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task EnsureCanDeleteAsync(string questionId, CancellationToken cancellationToken = default)
    {
        if (await _examRepository.ExistsByQuestionIdAsync(questionId, cancellationToken))
            throw new ExamDomainException("Cannot delete question: it is still used by an exam.");
    }
}
