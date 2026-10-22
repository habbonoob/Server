using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ObjectDataEntry</summary>
[WireStruct]
public sealed partial record ObjectDataEntry(int ItemId, FurniStuffData StuffData);
