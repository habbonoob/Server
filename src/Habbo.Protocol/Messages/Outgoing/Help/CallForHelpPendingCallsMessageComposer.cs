using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>CallForHelpPendingCallsMessageComposer</summary>
[Message(ServerMessages.CallForHelpPendingCallsMessageComposer)]
public sealed partial record CallForHelpPendingCallsMessageComposer(IReadOnlyList<PendingCallForHelpData> Calls);
