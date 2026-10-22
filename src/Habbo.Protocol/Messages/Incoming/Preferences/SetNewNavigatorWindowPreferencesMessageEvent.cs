using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>1292 SetNewNavigatorWindowPreferences — where the navigator sits and how it looks.</summary>
[Incoming(ClientMessages.SetNewNavigatorWindowPreferencesMessageEvent)]
public sealed partial record SetNewNavigatorWindowPreferencesMessageEvent(
    int X, int Y, int Width, int Height, bool LeftPanelHidden, int ResultsMode);
