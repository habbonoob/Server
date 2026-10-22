namespace Habbo.Domain.Users;

/// <summary>A badge a player owns. The id is the badge's own number on the wire; the slot is where
/// the player wears it on their profile (1-5), or <see cref="NotWorn"/>.</summary>
public sealed class UserBadge
{
    public const int NotWorn = 0;

    /// <summary>The slots a player can wear badges in; the client always sends all five.</summary>
    public const int WornSlots = 5;

    public const int CodeMaxLength = 64;

    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Code { get; set; }

    public int Slot { get; set; } = NotWorn;

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
