using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>2565 SetSoundSettings — the three volume sliders.</summary>
[Incoming(ClientMessages.SetSoundSettingsEvent)]
public sealed partial record SetSoundSettingsEvent(int TraxVolume, int FurniVolume, int UiVolume);
