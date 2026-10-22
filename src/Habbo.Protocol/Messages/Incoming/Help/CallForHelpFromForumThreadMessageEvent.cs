using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>CallForHelpFromForumThreadMessageEvent</summary>
[Incoming(ClientMessages.CallForHelpFromForumThreadMessageEvent)]
public sealed partial record CallForHelpFromForumThreadMessageEvent(int GroupId, int ThreadId, int TopicId, string Message, string ReporterName, string ReporterEmail);
