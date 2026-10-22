using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Tracking;

/// <summary>222 LatencyPingResponseMessageComposer — the request id echoed back so the client
/// can measure the round trip.</summary>
[Message(ServerMessages.LatencyPingResponseMessageComposer)]
public sealed partial record LatencyPingResponseMessageComposer(int RequestId);
