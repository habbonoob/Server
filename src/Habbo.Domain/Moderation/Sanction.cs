using Habbo.Domain.Users;

namespace Habbo.Domain.Moderation;

/// <summary>Something staff did to a user: a caution, a mute, a ban, a trade lock, a kick or a
/// message, and for how long.</summary>
public sealed class Sanction
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public SanctionKind Kind { get; set; }

    /// <summary>The step of the sanction ladder it was, when it was one.</summary>
    public int? SanctionTypeId { get; set; }

    public int TopicId { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? ModeratorId { get; set; }

    public int? IssueId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When it stops; null for one that takes effect and is done (a caution, a kick).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>A ban on the avatar alone, not the machine it plays from.</summary>
    public bool AvatarOnly { get; set; }

    /// <summary>The machine a ban also covers.</summary>
    public string MachineId { get; set; } = string.Empty;
}
