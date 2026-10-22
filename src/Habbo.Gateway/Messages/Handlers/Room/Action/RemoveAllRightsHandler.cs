using Habbo.Protocol.Messages.Incoming.Room.Action;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>863 RemoveAllRights — [roomId].</summary>
internal sealed class RemoveAllRightsHandler(IGrainFactory grains) : IMessageHandler<RemoveAllRightsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveAllRightsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var removed = await grains.GetGrain<IGameRoomGrain>(message.RoomId).RemoveAllRightsAsync(player.Id);
        foreach (var accountId in removed.AccountIds)
            await context.SendAsync(new FlatControllerRemovedComposer(removed.RoomId, accountId), ct);
        return true;
    }
}
