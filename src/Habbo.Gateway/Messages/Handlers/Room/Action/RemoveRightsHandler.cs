using Habbo.Protocol.Messages.Incoming.Room.Action;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>2873 RemoveRights — in the room the owner is in.</summary>
internal sealed class RemoveRightsHandler : IMessageHandler<RemoveRightsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveRightsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        var removed = await room.RemoveRightsAsync(player.Id, message.UserIds);
        foreach (var accountId in removed.AccountIds)
            await context.SendAsync(new FlatControllerRemovedComposer(removed.RoomId, accountId), ct);
        return true;
    }
}
