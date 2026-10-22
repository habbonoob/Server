using Habbo.Protocol.Messages.Incoming.Room.Chat;

namespace Habbo.Gateway.Messages.Handlers.Room.Chat;

/// <summary>CancelTypingMessageEvent — The avatar stopped typing.</summary>
internal sealed class CancelTypingHandler : IMessageHandler<CancelTypingMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CancelTypingMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetTypingAsync(player.Id, typing: false);
        return true;
    }
}
