using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.DeleteQuestion;

public record DeleteQuestionCommand(string Id, Actor Actor) : IRequest;
