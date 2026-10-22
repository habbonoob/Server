using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionReportMessageEvent — report the guide</summary>
[Incoming(ClientMessages.GuideSessionReportMessageEvent)]
public sealed partial record GuideSessionReportMessageEvent(string Message);
