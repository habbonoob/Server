using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Tracking;

/// <summary>2330 LatencyPingRequest — the id to echo back.</summary>
[Incoming(ClientMessages.LatencyPingRequestMessageEvent)]
public sealed partial record LatencyPingRequestMessageEvent(int RequestId);
