namespace Habbo.Silo.Rooms;

/// <summary>The group a user shows beside their name: its id, name and badge, and what they are
/// to it (owner, admin or member).</summary>
public sealed record FavouriteGroup(int Id, string Name, string BadgeCode, int Status)
{
    /// <summary>What the room says of a user who shows no group.</summary>
    public const int None = 0;
}
