using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Rooms;

/// <summary>Someone waiting outside a full room, in one of its lines.</summary>
public sealed record QueuedVisitor(Guid PlayerId, Guid SessionGuid, PlayerData Player, int Target)
{
    /// <summary>Club members have a line of their own, which moves first.</summary>
    public bool IsClub => Player.ClubLevel > ClubLevel.None;
}
