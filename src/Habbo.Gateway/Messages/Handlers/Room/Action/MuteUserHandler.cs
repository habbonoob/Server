using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>MuteUserMessageEvent — mute a user in the room.</summary>
internal sealed class MuteUserHandler : IMessageHandler<MuteUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MuteUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.MuteUserAsync(player.Id, message.UserId, message.Minutes);
        return true;
    }
}
