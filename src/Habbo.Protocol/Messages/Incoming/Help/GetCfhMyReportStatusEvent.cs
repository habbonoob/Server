using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GetCfhMyReportStatusEvent</summary>
[Incoming(ClientMessages.GetCfhMyReportStatusEvent)]
public sealed partial record GetCfhMyReportStatusEvent;
