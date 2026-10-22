using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Tracking;

/// <summary>LatencyPingReportMessageEvent — the client's measured latency</summary>
[Incoming(ClientMessages.LatencyPingReportMessageEvent)]
public sealed partial record LatencyPingReportMessageEvent(int Average, int ValidAverage, int Count);
