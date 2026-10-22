using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromForumMessageMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpFromForumMessageMessageEvent)]
public sealed partial record CallForHelpFromForumMessageMessageEvent(int GroupId, int ThreadId, int MessageId, int TopicId, string Message, string ReporterName, string ReporterEmail);
