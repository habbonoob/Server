using Habbo.Protocol.Messages.Incoming.Inventory.Purse;
using Habbo.Protocol.Messages.Outgoing.Inventory.Purse;
using Habbo.Silo.Abstractions.Players;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Purse;

/// <summary>3719 GetCreditsInfo — answer the purse with the player's credit balance, and their emeralds, which the
/// client keeps beside it. habbo.com sends EmeraldBalance right after CreditBalance (3 of 3 recorded official
/// logins, 2026-10-12 sessions).</summary>
internal sealed class GetCreditsInfoHandler(IGrainFactory grains) : IMessageHandler<GetCreditsInfoEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetCreditsInfoEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(new CreditBalanceComposer(player.Credits), ct);
        await grains.GetGrain<IPlayerGrain>(player.Id).SendEmeraldBalanceAsync();
        return true;
    }
}
