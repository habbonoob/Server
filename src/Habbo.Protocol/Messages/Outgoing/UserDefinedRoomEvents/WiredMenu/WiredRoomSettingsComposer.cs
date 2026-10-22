using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3966 WiredRoomSettingsComposer — who may change and read the room's wired, and its time zone.</summary>
[Message(ServerMessages.WiredRoomSettingsComposer)]
public sealed partial record WiredRoomSettingsComposer(int ModifyPermissionMask, int ReadPermissionMask, string Timezone);
