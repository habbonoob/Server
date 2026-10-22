using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>BadgeLeaderboardResult (September 2026) — a page of a badge leaderboard, how many it ranks, and the
/// asker's own place.</summary>
[Message(ServerMessages.BadgeLeaderboardResultMessageComposer)]
public sealed partial record BadgeLeaderboardResultMessageComposer(
    int Type,
    int Rarity,
    int Page,
    int PageSize,
    int TotalEntries,
    IReadOnlyList<BadgeLeaderboardEntryData> Entries,
    [property: WirePresence] BadgeLeaderboardEntryData? Own);
