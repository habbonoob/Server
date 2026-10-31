using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>A wired label in the room: the furni it is on, what it says, its bubble colour, who sees it and whether
/// it stays open.</summary>
[GenerateSerializer]
public sealed record RoomLabelData(
    [property: Id(0)] int ItemId,
    [property: Id(1)] string Text,
    [property: Id(2)] int BubbleColor,
    [property: Id(3)] int Visibility,
    [property: Id(4)] bool AlwaysOpen);
