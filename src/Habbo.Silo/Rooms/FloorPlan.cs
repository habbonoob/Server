namespace Habbo.Silo.Rooms;

/// <summary>A floor plan as the editor saves it: the heightmap, the door, and the walls and floor.</summary>
public sealed record FloorPlan(
    string Heightmap, int DoorX, int DoorY, int DoorDirection, int WallThickness, int FloorThickness, int FixedWallsHeight);
