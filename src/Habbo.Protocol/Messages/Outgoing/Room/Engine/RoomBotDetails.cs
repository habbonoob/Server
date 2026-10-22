using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>The trailing fields a bot with skills carries: who owns it and the skills its menu offers.</summary>
[WireStruct]
public sealed partial record RoomBotDetails(
    string Sex,
    int OwnerId,
    string OwnerName,
    [property: WireFormat(WireType.I16)] IReadOnlyList<short> Skills);
