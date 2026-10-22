using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>CallForHelpPendingCallsDeletedMessageComposer — the player's pending calls were deleted. It has no body.</summary>
[Message(ServerMessages.CallForHelpPendingCallsDeletedMessageComposer)]
public sealed partial record CallForHelpPendingCallsDeletedMessageComposer;
