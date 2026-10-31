using Habbo.Protocol.Messages.Incoming.RewardTrack;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.RewardTrack;

/// <summary>PurchaseRewardTrackPremiumMessageEvent — buy a reward track's premium side.</summary>
internal sealed class PurchaseRewardTrackPremiumHandler(IGrainFactory grains) : IMessageHandler<PurchaseRewardTrackPremiumMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchaseRewardTrackPremiumMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is { } player)
            await grains.GetGrain<IPlayerGrain>(player.Id).PurchaseRewardTrackPremiumAsync(message.TrackId);
        return true;
    }
}
