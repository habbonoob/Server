using Habbo.Protocol.Messages.Incoming.Tracking;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages.Handlers.Tracking;

/// <summary>3803 EventLog — client-side analytics spam; absorbed.</summary>
[AllowUnauthenticated]
internal sealed class EventLogHandler(ILogger<EventLogHandler> logger)
    : IMessageHandler<EventLogMessageEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, EventLogMessageEvent message, CancellationToken ct)
    {
        logger.LogDebug("Connection {ConnectionId}: event log {Category}/{Action}/{Label}",
            context.Session.Id, message.Category, message.Action, message.Label);
        return Task.FromResult(true);
    }
}
