using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RewardTrack;

/// <summary>RewardTrackPremiumPurchaseResult (September 2026) — how buying a track's premium went, and the player's
/// points after it: 0 bought, 1 tracks off, 2 no such track, 3 not eligible, 4 the track sells none, 5 owned already,
/// 6 its price is wrong, 7 not enough credits, 8 not enough diamonds, 9 failed.</summary>
[Message(ServerMessages.RewardTrackPremiumPurchaseResultComposer)]
public sealed partial record RewardTrackPremiumPurchaseResultComposer(
    string TrackId,
    int ResultCode,
    int Points)
{
    public const int Bought = 0;
    public const int Disabled = 1;
    public const int NoSuchTrack = 2;
    public const int NotEligible = 3;
    public const int NotSold = 4;
    public const int AlreadyOwned = 5;
    public const int InvalidPrice = 6;
    public const int NotEnoughCredits = 7;
    public const int NotEnoughDiamonds = 8;
    public const int Failed = 9;
}
