using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>LockAllChestsMessageEvent — lock or unlock chests in the room.</summary>
internal sealed class LockAllChestsHandler : IMessageHandler<LockAllChestsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, LockAllChestsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.LockAllChestsAsync(player.Id, message.Lock, message.AllChests);
        return true;
    }
}
