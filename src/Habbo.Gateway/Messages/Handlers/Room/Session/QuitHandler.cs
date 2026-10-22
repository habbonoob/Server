using Habbo.Protocol.Messages.Incoming.Room.Session;

namespace Habbo.Gateway.Messages.Handlers.Room.Session;

/// <summary>3981 Quit — leave the room the session is in.</summary>
internal sealed class QuitHandler : IMessageHandler<QuitMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, QuitMessageEvent message, CancellationToken ct)
    {
        await context.Session.LeaveRoomAsync();
        return true;
    }
}
