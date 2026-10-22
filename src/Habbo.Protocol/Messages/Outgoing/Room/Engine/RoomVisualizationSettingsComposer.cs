using Habbo.Domain.Rooms;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1145 RoomVisualizationSettingsComposer.</summary>
[Message(ServerMessages.RoomVisualizationSettingsComposer)]
public sealed partial record RoomVisualizationSettingsComposer(
    bool WallsHidden, int WallThicknessExponent, int FloorThicknessExponent)
{
    public static RoomVisualizationSettingsComposer From(RoomVisualizationData visualization) =>
        new(visualization.WallsHidden,
            visualization.WallThicknessExponent,
            visualization.FloorThicknessExponent);
}
