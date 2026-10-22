using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>1465 RespectUser — respect somebody in the same room.</summary>
internal sealed class RespectUserHandler : IMessageHandler<RespectUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RespectUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.RespectAsync(player.Id, message.UserId);
        return true;
    }
}
