using Habbo.Protocol.Messages.Incoming.Room.Action;
using Habbo.Protocol.Messages.Outgoing.RoomSettings;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>1818 AssignRights — in the room the owner is in.</summary>
internal sealed class AssignRightsHandler : IMessageHandler<AssignRightsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AssignRightsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        if (await room.AssignRightsAsync(player.Id, message.UserId) is { } added)
            await context.SendAsync(new FlatControllerAddedComposer(added.RoomId,
                new FlatControllerData(added.Controller.AccountId, added.Controller.Username)), ct);
        return true;
    }
}
