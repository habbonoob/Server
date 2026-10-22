using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpMessageEvent)]
public sealed partial record CallForHelpMessageEvent(string Message, int TopicId, int ReportedUserId, int RoomId, IReadOnlyList<ReportedChatLineData> ChatLines, string ReporterName, string ReporterEmail);
