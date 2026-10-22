using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromIMMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpFromIMMessageEvent)]
public sealed partial record CallForHelpFromIMMessageEvent(string Message, int TopicId, int ReportedUserId, IReadOnlyList<ReportedChatLineData> ChatLines, string ReporterName, string ReporterEmail);
