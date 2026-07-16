using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.UserAggregate.Commands.CreateUser;

public record CreateUserCommand(string Email, string FirstName, string LastName, UserRole Role, Actor Actor) : IRequest<CreateUserResponse>;
