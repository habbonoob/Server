using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>627 NewNavigatorInit — asks for the navigator's tabs.</summary>
[Incoming(ClientMessages.NewNavigatorInitEvent)]
public sealed partial record NewNavigatorInitEvent;
