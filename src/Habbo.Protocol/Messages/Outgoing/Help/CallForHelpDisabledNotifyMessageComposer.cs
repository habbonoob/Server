using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>CallForHelpDisabledNotifyMessageComposer</summary>
[Message(ServerMessages.CallForHelpDisabledNotifyMessageComposer)]
public sealed partial record CallForHelpDisabledNotifyMessageComposer(string InfoUrl);
