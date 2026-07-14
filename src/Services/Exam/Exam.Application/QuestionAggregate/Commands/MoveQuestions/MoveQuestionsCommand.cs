using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.MoveQuestions;

public record MoveQuestionsCommand(IReadOnlyCollection<string> QuestionIds, string TargetCategoryId, Actor Actor) : IRequest;
