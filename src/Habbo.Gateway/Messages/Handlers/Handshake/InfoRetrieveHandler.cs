using Habbo.Protocol.Messages.Incoming.Handshake;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>1484 InfoRetrieve — the client asks for its own user data. Everything else the
/// hotel view needs is pushed right after the login is accepted, or answered by the request
/// that asks for it, so this carries only the user.</summary>
internal sealed class InfoRetrieveHandler : IMessageHandler<InfoRetrieveMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, InfoRetrieveMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(UserObjectComposer.From(player, player.LastAccess), ct);
        return true;
    }
}
