using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RewardTrack;

/// <summary>PurchaseRewardTrackPremium (September 2026) — the player buys a reward track's premium side.</summary>
[Incoming(ClientMessages.PurchaseRewardTrackPremiumMessageEvent)]
public sealed partial record PurchaseRewardTrackPremiumMessageEvent(
    string TrackId);
