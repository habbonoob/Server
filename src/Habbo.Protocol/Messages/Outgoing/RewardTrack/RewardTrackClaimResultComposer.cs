using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RewardTrack;

/// <summary>RewardTrackClaimResult (September 2026) — how claiming a prize went: 0 claimed, 1 tracks off, 2 no such
/// track, 3 no such prize, 4 not eligible, 5 not enough points, 6 claimed already, 7 failed, 8 premium needed.</summary>
[Message(ServerMessages.RewardTrackClaimResultComposer)]
public sealed partial record RewardTrackClaimResultComposer(
    string TrackId,
    string PrizeId,
    int ResultCode)
{
    public const int Claimed = 0;
    public const int Disabled = 1;
    public const int NoSuchTrack = 2;
    public const int NoSuchPrize = 3;
    public const int NotEligible = 4;
    public const int NotEnoughPoints = 5;
    public const int AlreadyClaimed = 6;
    public const int Failed = 7;
    public const int PremiumRequired = 8;
}
