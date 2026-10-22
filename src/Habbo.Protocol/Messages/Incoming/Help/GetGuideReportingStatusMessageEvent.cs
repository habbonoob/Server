using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GetGuideReportingStatusMessageEvent — whether the player may ask for help or report now</summary>
[Incoming(ClientMessages.GetGuideReportingStatusMessageEvent)]
public sealed partial record GetGuideReportingStatusMessageEvent;
