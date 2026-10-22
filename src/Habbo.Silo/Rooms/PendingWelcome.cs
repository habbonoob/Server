namespace Habbo.Silo.Rooms;

/// <summary>What is left of an entry once the player's client has the room: whatever speaks of the
/// room's avatars has to wait until it knows them. A visitor logger counts visits from the one before.</summary>
public readonly record struct PendingWelcome(bool IsReentry, DateTimeOffset? PreviousVisit);
