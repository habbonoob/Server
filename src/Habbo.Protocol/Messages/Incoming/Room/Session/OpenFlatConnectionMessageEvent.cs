using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Session;

/// <summary>3463 OpenFlatConnection — enter a room, with its password when it has one.</summary>
[Incoming(ClientMessages.OpenFlatConnectionMessageEvent)]
public sealed partial record OpenFlatConnectionMessageEvent(int RoomId, string Password, int Direction);
