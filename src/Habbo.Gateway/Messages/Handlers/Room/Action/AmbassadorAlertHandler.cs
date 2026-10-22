using Habbo.Protocol.Messages.Incoming.Room.Action;

namespace Habbo.Gateway.Messages.Handlers.Room.Action;

/// <summary>AmbassadorAlertMessageEvent — an ambassador warns a user.</summary>
internal sealed class AmbassadorAlertHandler : IMessageHandler<AmbassadorAlertMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AmbassadorAlertMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.AmbassadorAlertAsync(player.Id, message.UserId);
        return true;
    }
}
