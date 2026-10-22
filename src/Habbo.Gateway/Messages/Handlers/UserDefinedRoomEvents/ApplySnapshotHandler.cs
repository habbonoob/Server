using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>50 ApplySnapshot — a match-to-snapshot effect's "apply" button.</summary>
internal sealed class ApplySnapshotHandler : IMessageHandler<ApplySnapshotMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ApplySnapshotMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ApplyWiredSnapshotAsync(player.Id, message.ItemId);
        return true;
    }
}
