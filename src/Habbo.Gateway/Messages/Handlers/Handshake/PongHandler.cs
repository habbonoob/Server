using Habbo.Protocol.Messages.Incoming.Handshake;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>1824 Pong — the client answering a ping: it is still there.</summary>
[AllowUnauthenticated]
internal sealed class PongHandler : IMessageHandler<PongMessageEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, PongMessageEvent message, CancellationToken ct)
    {
        context.Session.LastPongAt = DateTimeOffset.UtcNow;
        return Task.FromResult(true);
    }
}
