using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Furniture;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>3810 CreditFurniRedeem — the room pays out and destroys the item; whether it does is
/// the item's behaviour's call. The session's copy of the player is refreshed afterwards so the
/// balance it holds is not the one from before the exchange.</summary>
internal sealed class CreditFurniRedeemHandler : IMessageHandler<CreditFurniRedeemMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, CreditFurniRedeemMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room
            || context.Session.PlayerGrain is not { } playerGrain)
            return true;

        await room.UseItemAsync(player.Id, message.ItemId, FurniAction.Redeem, 0);
        context.Session.RefreshPlayer(await playerGrain.GetDataAsync());
        return true;
    }
}
