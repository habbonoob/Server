using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>1831 Sign.</summary>
internal sealed class SignHandler : IMessageHandler<SignMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SignMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ShowSignAsync(player.Id, message.Sign);
        return true;
    }
}
