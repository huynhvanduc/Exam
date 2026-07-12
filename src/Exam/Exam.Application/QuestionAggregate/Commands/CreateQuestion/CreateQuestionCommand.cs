using Exam.Domain.Enums;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.CreateQuestion;

public record CreateQuestionCommand(
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    int Points,
    string OwnerUserId) : IRequest<QuestionDto>;

public record AnswerInput(string Content, bool IsCorrect);
