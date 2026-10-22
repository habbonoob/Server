using Habbo.Protocol.Messages.Incoming.Handshake;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>1861 VersionCheck — the client reports its build number and external variables url. Logged only.</summary>
[AllowUnauthenticated]
internal sealed class VersionCheckHandler(ILogger<VersionCheckHandler> logger)
    : IMessageHandler<VersionCheckMessageEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, VersionCheckMessageEvent message, CancellationToken ct)
    {
        logger.LogDebug("Connection {ConnectionId}: version check, build={Build}, url={Url}",
            context.Session.Id, message.BuildNumber, message.ExternalVariablesUrl);
        return Task.FromResult(true);
    }
}
