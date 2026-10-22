using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Sound;

/// <summary>2744 GetSoundSettings — the client asks for the player's own settings, volumes
/// among them. Answered with AccountPreferencesComposer, which carries all of them.</summary>
[Incoming(ClientMessages.GetSoundSettingsEvent)]
public sealed partial record GetSoundSettingsEvent;
