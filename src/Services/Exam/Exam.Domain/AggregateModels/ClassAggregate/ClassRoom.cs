using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ClassAggregate;

public class ClassRoom : Entity, IAggregateRoot
{
    private List<string> _memberUserIds = new();

    public string Name { get; private set; }

    public string JoinCode { get; private set; }

    public string OwnerUserId { get; private set; }

    public DateTime DateCreated { get; private set; }

    public IReadOnlyCollection<string> MemberUserIds
    {
        get => _memberUserIds;
        private set => _memberUserIds = value?.ToList() ?? new List<string>();
    }

    private ClassRoom()
    {
    }

    public static ClassRoom Create(string name, string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Class name is required.");

        if (string.IsNullOrWhiteSpace(ownerUserId))
            throw new ExamDomainException("Class owner is required.");

        return new ClassRoom
        {
            Name = name,
            OwnerUserId = ownerUserId,
            JoinCode = GenerateJoinCode(),
            DateCreated = DateTime.UtcNow
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExamDomainException("Class name is required.");

        Name = name;
    }

    public void RegenerateJoinCode() => JoinCode = GenerateJoinCode();

    public void AddMember(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ExamDomainException("User id is required.");

        if (userId == OwnerUserId)
            throw new ExamDomainException("The class owner cannot join their own class as a member.");

        if (!_memberUserIds.Contains(userId))
            _memberUserIds.Add(userId);
    }

    public void RemoveMember(string userId) => _memberUserIds.Remove(userId);

    public bool HasMember(string userId) => _memberUserIds.Contains(userId);

    private static string GenerateJoinCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // bỏ ký tự dễ nhầm: 0/O, 1/I
        var buffer = new char[6];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = alphabet[Random.Shared.Next(alphabet.Length)];
        return new string(buffer);
    }
}
