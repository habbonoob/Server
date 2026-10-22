using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>A user with rights in a room.</summary>
[WireStruct]
public sealed partial record FlatControllerData(int UserId, string UserName);
