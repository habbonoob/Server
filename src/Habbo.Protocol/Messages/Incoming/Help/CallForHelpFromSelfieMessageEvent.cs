using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromSelfieMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpFromSelfieMessageEvent)]
public sealed partial record CallForHelpFromSelfieMessageEvent(string SelfieId, int RoomId, int ReportedUserId, string Message, int TopicId);
