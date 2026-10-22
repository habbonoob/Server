namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>The group a room is home to, as room listings show it.</summary>
[GenerateSerializer]
public sealed partial record RoomGroup([property: Id(0)] int GroupId, [property: Id(1)] string Name, [property: Id(2)] string BadgeCode);
