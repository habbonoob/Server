using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>NavigatorDeleteSavedSearchEvent</summary>
[Incoming(ClientMessages.NavigatorDeleteSavedSearchEvent)]
public sealed partial record NavigatorDeleteSavedSearchEvent(int SearchId);
