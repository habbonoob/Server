using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>DeletePendingCallsForHelpMessageEvent</summary>
[Incoming(ClientMessages.DeletePendingCallsForHelpMessageEvent)]
public sealed partial record DeletePendingCallsForHelpMessageEvent;
