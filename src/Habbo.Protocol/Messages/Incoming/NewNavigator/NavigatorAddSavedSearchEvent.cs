using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>NavigatorAddSavedSearchEvent</summary>
[Incoming(ClientMessages.NavigatorAddSavedSearchEvent)]
public sealed partial record NavigatorAddSavedSearchEvent(string SearchCode, string Filter);
