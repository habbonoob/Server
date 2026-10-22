using Habbo.Protocol.Messages.Incoming.Handshake;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>2780 Disconnect — the client is going away; close the connection.</summary>
[AllowUnauthenticated]
internal sealed class DisconnectHandler : IMessageHandler<DisconnectMessageEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, DisconnectMessageEvent message, CancellationToken ct)
        => Task.FromResult(false);
}
