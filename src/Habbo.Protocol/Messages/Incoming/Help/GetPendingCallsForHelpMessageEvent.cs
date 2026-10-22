using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GetPendingCallsForHelpMessageEvent</summary>
[Incoming(ClientMessages.GetPendingCallsForHelpMessageEvent)]
public sealed partial record GetPendingCallsForHelpMessageEvent;
