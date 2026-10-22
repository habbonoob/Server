using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetBadgeLeaderboard (September 2026) — [type, rarity, page, pageSize]: a page of a badge leaderboard
/// (0 total badges, 1 badges of the rarity, 2 achievement level).</summary>
[Incoming(ClientMessages.GetBadgeLeaderboardMessageEvent)]
public sealed partial record GetBadgeLeaderboardMessageEvent(int Type, int Rarity, int Page, int PageSize);
