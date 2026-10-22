namespace Habbo.Domain.Rooms;

/// <summary>A room layout template: heightmap plus door coordinates.</summary>
public sealed class RoomModelData
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Heightmap { get; set; }
    public int DoorX { get; set; }
    public int DoorY { get; set; }
    public int DoorDirection { get; set; }
}
