namespace Habbo.Domain.Users;

/// <summary>A look saved in one of the wardrobe's slots. Slots 1-5 are the club's, 6-10 VIP's,
/// as the avatar editor unlocks them.</summary>
public sealed class UserOutfit
{
    public const int FirstSlot = 1;
    public const int LastClubSlot = 5;
    public const int LastSlot = 10;

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int Slot { get; set; }

    public required string Figure { get; set; }
    public Gender Gender { get; set; }
}
