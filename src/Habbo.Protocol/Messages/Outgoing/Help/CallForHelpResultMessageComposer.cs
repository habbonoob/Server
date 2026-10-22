using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>CallForHelpResultMessageComposer</summary>
[Message(ServerMessages.CallForHelpResultMessageComposer)]
public sealed partial record CallForHelpResultMessageComposer(int ResultType, string MessageText);
