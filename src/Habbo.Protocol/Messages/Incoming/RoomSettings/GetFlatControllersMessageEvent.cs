using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>3832 GetFlatControllers — who has rights in the room, for its settings.</summary>
[Incoming(ClientMessages.GetFlatControllersMessageEvent)]
public sealed partial record GetFlatControllersMessageEvent(int RoomId);
