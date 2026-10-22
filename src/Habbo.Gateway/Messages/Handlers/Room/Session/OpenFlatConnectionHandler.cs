using Habbo.Protocol.Messages.Incoming.Room.Session;
using Orleans;

namespace Habbo.Gateway.Messages.Handlers.Room.Session;

/// <summary>3463 OpenFlatConnection — enter the room and push the entry sequence (<see cref="RoomEntry"/>). The
/// client sends it when the server did not open the room itself on the navigator's forward: a doorbell, a
/// password, or a room reached another way.</summary>
internal sealed class OpenFlatConnectionHandler(IGrainFactory grains)
    : IMessageHandler<OpenFlatConnectionMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, OpenFlatConnectionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is null)
            return false;

        await RoomEntry.EnterAsync(grains, context, message.RoomId, message.Password, ct);
        return true;
    }
}
