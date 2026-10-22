using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>PassCarryItemMessageEvent — Hands what the avatar carries to another user.</summary>
internal sealed class PassCarryItemHandler : IMessageHandler<PassCarryItemMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PassCarryItemMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.PassHandItemAsync(player.Id, message.UserId);
        return true;
    }
}
