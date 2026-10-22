using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>2949 GamePlayerValueMessageComposer — a number shown over an avatar: a Freeze player's lives.</summary>
[Message(ServerMessages.GamePlayerValueMessageComposer)]
public sealed partial record GamePlayerValueMessageComposer(int RoomIndex, int Value);
