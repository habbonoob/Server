using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RewardTrack;

/// <summary>ClaimRewardTrackPrize (September 2026) — the player claims a prize of a reward track.</summary>
[Incoming(ClientMessages.ClaimRewardTrackPrizeMessageEvent)]
public sealed partial record ClaimRewardTrackPrizeMessageEvent(
    string TrackId,
    string PrizeId);
