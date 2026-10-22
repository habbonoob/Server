using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>CancelEventMessageEvent</summary>
[Incoming(ClientMessages.CancelEventMessageEvent)]
public sealed partial record CancelEventMessageEvent(int EventId);
