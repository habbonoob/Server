namespace Habbo.Silo.Rooms;

/// <summary>Two friends deciding whether to lock a friendship furni: who started, who was asked, and who has confirmed.</summary>
public sealed class FriendLock(int itemId, Guid starter, Guid friend)
{
    public int ItemId { get; } = itemId;

    public Guid Starter { get; } = starter;

    public Guid Friend { get; } = friend;

    public HashSet<Guid> Confirmed { get; } = [];

    public bool Involves(Guid playerId) => playerId == Starter || playerId == Friend;

    public Guid PartnerOf(Guid playerId) => playerId == Starter ? Friend : Starter;
}
