using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredMenu;

/// <summary>3868 WiredPermissionsComposer — whether the user may change, and read, the room's wired.</summary>
[Message(ServerMessages.WiredPermissionsComposer)]
public sealed partial record WiredPermissionsComposer(bool CanModify, bool CanRead);
