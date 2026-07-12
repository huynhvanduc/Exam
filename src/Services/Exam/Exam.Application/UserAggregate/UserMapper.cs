namespace Exam.Application.UserAggregate;

internal static class UserMapper
{
    public static UserDto ToDto(Domain.AggregateModels.UserAggregate.User user) =>
        new(user.Id, user.ExternalId, user.FirstName, user.LastName, user.Role);
}
