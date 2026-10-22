namespace Habbo.Silo.Abstractions.Groups;

/// <summary>What the group creation wizard sends.</summary>
[GenerateSerializer]
public sealed partial record GroupDraft(
    [property: Id(0)] string Name,
    [property: Id(1)] string Description,
    [property: Id(2)] int RoomId,
    [property: Id(3)] int PrimaryColorId,
    [property: Id(4)] int SecondaryColorId,
    [property: Id(5)] IReadOnlyList<int> BadgeData);
