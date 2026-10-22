using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1819 ObjectDataUpdateMessageComposer — a floor item's data changed: a lamp switched, a
/// scoreboard ticked. The client reads the id as a string and parses it back to a number.</summary>
[Message(ServerMessages.ObjectDataUpdateMessageComposer)]
public sealed partial record ObjectDataUpdateMessageComposer(
    [property: WireFormat(WireType.IntegerString)] int ItemId,
    FurniStuffData StuffData);
