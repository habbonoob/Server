using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>2051 ChangePosture — [posture] 1 = sit (on a sittable tile), 0 = stand.</summary>
internal sealed class ChangePostureHandler : IMessageHandler<ChangePostureMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, ChangePostureMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.ChangePostureAsync(player.Id, message.Posture);
        return true;
    }
}
