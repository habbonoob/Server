using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One area a piece of furniture hides, as FloorHeightMap carries it.</summary>
[WireStruct]
public sealed partial record AreaHideWireData(
    int FurniId, bool On, int RootX, int RootY, int Width, int Length, bool Invert);
