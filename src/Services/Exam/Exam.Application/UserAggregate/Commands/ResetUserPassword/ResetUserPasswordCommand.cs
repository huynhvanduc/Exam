using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.ResetUserPassword;

public record ResetUserPasswordCommand(string ExternalId, Actor Actor) : IRequest<ResetUserPasswordResponse>;
