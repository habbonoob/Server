using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>LetUserInMessageEvent — answer the doorbell.</summary>
internal sealed class LetUserInHandler : IMessageHandler<LetUserInMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, LetUserInMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.LetUserInAsync(player.Id, message.UserName, message.Allow);
        return true;
    }
}
