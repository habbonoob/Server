using Habbo.Protocol.Messages.Incoming.Handshake;
using Habbo.Protocol.Releases;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>4000 ClientHello — the client announces its release and flash version. The release picks the headers
/// the connection speaks: every release renumbers its messages, and ClientHello is 4000 in all of them.</summary>
[PreCrypto]
internal sealed class ClientHelloHandler(ILogger<ClientHelloHandler> logger)
    : IMessageHandler<ClientHelloMessageEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, ClientHelloMessageEvent message, CancellationToken ct)
    {
        logger.LogInformation("Connection {ConnectionId}: client hello, release={Release}, flash={Flash}",
            context.Session.Id, message.Release, message.Type);

        if (ReleaseHeaderMap.For(message.Release) is { } headers)
            context.Session.Headers = headers;
        else
            logger.LogWarning("Connection {ConnectionId}: no headers for release {Release}; speaking {Native}'s",
                context.Session.Id, message.Release, ReleaseHeaders.Native);
        return Task.FromResult(true);
    }
}
