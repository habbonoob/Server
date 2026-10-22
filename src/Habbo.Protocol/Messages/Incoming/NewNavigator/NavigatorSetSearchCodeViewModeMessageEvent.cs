using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>NavigatorSetSearchCodeViewModeMessageEvent</summary>
[Incoming(ClientMessages.NavigatorSetSearchCodeViewModeMessageEvent)]
public sealed partial record NavigatorSetSearchCodeViewModeMessageEvent(string SearchCode, int ViewMode);
