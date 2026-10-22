using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Navigator;

/// <summary>EditEventMessageEvent</summary>
[Incoming(ClientMessages.EditEventMessageEvent)]
public sealed partial record EditEventMessageEvent(int EventId, string Name, string Description);
