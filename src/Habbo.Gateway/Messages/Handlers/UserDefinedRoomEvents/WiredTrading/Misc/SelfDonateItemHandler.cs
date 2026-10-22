using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Misc;
using Habbo.Silo.Abstractions.Inventory;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Misc;

/// <summary>SelfDonateItemMessageEvent — give oneself items, in the sandbox.</summary>
internal sealed class SelfDonateItemHandler(ISelfDonationService service) : IMessageHandler<SelfDonateItemMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SelfDonateItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.DonateAsync(player, message.IsWallItem, message.TypeId, message.PosterId, message.Amount);
        return true;
    }
}
