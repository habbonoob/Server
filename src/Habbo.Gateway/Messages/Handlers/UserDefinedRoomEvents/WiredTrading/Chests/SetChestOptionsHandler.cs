using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestOptionsMessageEvent — a chest's lock and capacity.</summary>
internal sealed class SetChestOptionsHandler : IMessageHandler<SetChestOptionsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetChestOptionsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetChestOptionsAsync(player.Id, message.ChestId, message.Locked, message.AutoLock, message.Capacity);
        return true;
    }
}
