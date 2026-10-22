using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>UnmuteUserMessageEvent — unmute a user in the room.</summary>
internal sealed class UnmuteUserHandler : IMessageHandler<UnmuteUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnmuteUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.UnmuteUserAsync(player.Id, message.UserId);
        return true;
    }
}
