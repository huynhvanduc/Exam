using Exam.Application.Exceptions;

namespace Exam.Application.Common;

public static class OwnershipGuard
{
    public static void EnsureOwnerOrAdmin(Actor actor, string ownerUserId, string entityName, string entityId)
    {
        if (actor.Role == UserRole.Admin)
            return;

        if (actor.UserId == ownerUserId)
            return;

        throw ForbiddenException.NotOwner(entityName, entityId);
    }
}
