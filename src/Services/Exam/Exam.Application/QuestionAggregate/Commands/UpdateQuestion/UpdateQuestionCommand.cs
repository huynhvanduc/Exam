using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.UpdateQuestion;

public record UpdateQuestionCommand(
    string Id,
    string Content,
    QuestionType QuestionType,
    Level Level,
    string CategoryId,
    IReadOnlyCollection<AnswerInput> Answers,
    string Explain,
    int Points,
    Actor Actor) : IRequest<QuestionDto>;
