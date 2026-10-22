namespace Habbo.Domain.Users;

/// <summary>A code that lets one person register while sign-up is closed to everyone else. Staff make them and hand
/// them out; each is used once.</summary>
public sealed class InviteCode
{
    public const int CodeLength = 12;
    public const int NoteMaxLength = 128;

    public required string Code { get; set; }

    /// <summary>Who the code is meant for, as its maker noted it ("for mum").</summary>
    public string Note { get; set; } = string.Empty;

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The account registered with it; null while it is unused.</summary>
    public Guid? UsedById { get; set; }
    public User? UsedBy { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public bool IsUsed => UsedById is not null;
}
