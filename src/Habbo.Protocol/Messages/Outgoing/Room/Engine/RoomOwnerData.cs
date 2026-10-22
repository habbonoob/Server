using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One entry of the owner-name map the item packets are preceded by.</summary>
[WireStruct]
public sealed partial record RoomOwnerData(int AccountId, string Name);
