using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Preferences;

/// <summary>3187 SetRoomCameraPreferences — whether the camera follows the avatar.</summary>
[Incoming(ClientMessages.SetRoomCameraPreferencesMessageEvent)]
public sealed partial record SetRoomCameraPreferencesMessageEvent(bool FollowDisabled);
