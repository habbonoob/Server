using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>709 RoomPropertyMessageComposer — one key/value pair per message.</summary>
[Message(ServerMessages.RoomPropertyMessageComposer)]
public sealed partial record RoomPropertyMessageComposer(string Key, string Value);
