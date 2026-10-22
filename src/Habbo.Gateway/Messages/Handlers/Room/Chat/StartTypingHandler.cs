using Habbo.Protocol.Messages.Incoming.Room.Chat;

namespace Habbo.Gateway.Messages.Handlers.Room.Chat;

/// <summary>StartTypingMessageEvent — The avatar started typing.</summary>
internal sealed class StartTypingHandler : IMessageHandler<StartTypingMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, StartTypingMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetTypingAsync(player.Id, typing: true);
        return true;
    }
}
