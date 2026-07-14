using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.CreateQuestion;

public record CreateQuestionCommand(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    string OwnerUserId) : IRequest<QuestionDto>;
