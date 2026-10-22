using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3842 UserRemoveMessageComposer — the room index as a string.</summary>
[Message(ServerMessages.UserRemoveMessageComposer)]
public sealed partial record UserRemoveMessageComposer(
    [property: WireFormat(WireType.IntegerString)] int RoomIndex);
