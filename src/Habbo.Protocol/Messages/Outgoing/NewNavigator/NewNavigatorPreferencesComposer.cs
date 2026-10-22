using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.NewNavigator;

/// <summary>NewNavigatorPreferencesComposer — where the navigator window sits and how it shows results</summary>
[Message(ServerMessages.NewNavigatorPreferencesComposer)]
public sealed partial record NewNavigatorPreferencesComposer(int WindowX, int WindowY, int WindowWidth, int WindowHeight, bool LeftPaneHidden, int ResultsMode);
