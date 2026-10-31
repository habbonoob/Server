using Habbo.Protocol.Messages.Incoming.RewardTrack;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RewardTrack;

/// <summary>ClaimRewardTrackPrizeMessageEvent — claim a reward track prize.</summary>
internal sealed class ClaimRewardTrackPrizeHandler(IGrainFactory grains) : IMessageHandler<ClaimRewardTrackPrizeMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClaimRewardTrackPrizeMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is { } player)
            await grains.GetGrain<IPlayerGrain>(player.Id).ClaimRewardTrackPrizeAsync(message.TrackId, message.PrizeId);
        return true;
    }
}
