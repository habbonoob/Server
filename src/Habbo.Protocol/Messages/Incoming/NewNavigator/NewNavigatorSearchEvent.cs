using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.NewNavigator;

/// <summary>815 NewNavigatorSearch — a search code and the search box text.</summary>
[Incoming(ClientMessages.NewNavigatorSearchEvent)]
public sealed partial record NewNavigatorSearchEvent(string SearchCode, string Filter);
