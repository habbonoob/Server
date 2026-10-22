using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredMenu;

/// <summary>1325 WiredSetRoomSettings — who may change and who may read the room's wired (a bit
/// per permission level), and the room's wired time zone.</summary>
[Incoming(ClientMessages.WiredSetRoomSettingsMessageEvent)]
public sealed partial record WiredSetRoomSettingsMessageEvent(int ModifyPermissionMask, int ReadPermissionMask, string Timezone);
