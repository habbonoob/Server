using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Tracking;
using Habbo.Protocol.Messages.Outgoing.Tracking;

namespace Habbo.Gateway.Messages.Handlers.Tracking;

/// <summary>2330 LatencyPingRequest — echo the request id back.</summary>
internal sealed class LatencyPingRequestHandler : IMessageHandler<LatencyPingRequestMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, LatencyPingRequestMessageEvent message, CancellationToken ct)
    {
        await context.SendAsync(new LatencyPingResponseMessageComposer(message.RequestId), ct);
        return true;
    }
}
