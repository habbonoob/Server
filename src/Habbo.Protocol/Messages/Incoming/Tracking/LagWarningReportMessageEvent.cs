using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Tracking;

/// <summary>LagWarningReportMessageEvent — the client lagged</summary>
[Incoming(ClientMessages.LagWarningReportMessageEvent)]
public sealed partial record LagWarningReportMessageEvent(int WarningCount);
