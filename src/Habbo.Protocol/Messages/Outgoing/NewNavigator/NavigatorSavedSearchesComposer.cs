using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>NavigatorSavedSearchesComposer — the player's saved searches</summary>
[Message(ServerMessages.NavigatorSavedSearchesComposer)]
public sealed partial record NavigatorSavedSearchesComposer(IReadOnlyList<NavigatorQuickLinkData> Searches);
