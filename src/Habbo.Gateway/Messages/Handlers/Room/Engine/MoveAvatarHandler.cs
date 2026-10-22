using Habbo.Domain.Rooms;
using Habbo.Protocol.Messages.Incoming.Room.Engine;

namespace Habbo.Gateway.Messages.Handlers.Room.Engine;

/// <summary>2256 MoveAvatar — exactly two ints: destination x, y. The room grain pathfinds
/// and walks the avatar tile by tile.</summary>
internal sealed class MoveAvatarHandler : IMessageHandler<MoveAvatarMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, MoveAvatarMessageEvent message, CancellationToken ct)
    {
        var player = context.Session.Player;
        if (player is null || context.Session.Room is not { } room)
            return true;

        await room.MoveAsync(player.Id, message.X, message.Y);
        return true;
    }
}
