using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>CallForHelpReplyMessageComposer</summary>
[Message(ServerMessages.CallForHelpReplyMessageComposer)]
public sealed partial record CallForHelpReplyMessageComposer(string Message);
