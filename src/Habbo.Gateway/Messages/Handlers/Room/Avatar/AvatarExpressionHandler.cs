using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>2738 AvatarExpression — the room shows it, or puts the avatar to sleep.</summary>
internal sealed class AvatarExpressionHandler : IMessageHandler<AvatarExpressionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AvatarExpressionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ExpressAsync(player.Id, message.Expression);
        return true;
    }
}
