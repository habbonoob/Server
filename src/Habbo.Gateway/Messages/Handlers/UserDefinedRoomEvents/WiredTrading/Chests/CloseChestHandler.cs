using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>CloseChestMessageEvent — stop looking into a chest.</summary>
internal sealed class CloseChestHandler : IMessageHandler<CloseChestMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CloseChestMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.CloseChestAsync(player.Id, message.ChestId);
        return true;
    }
}
