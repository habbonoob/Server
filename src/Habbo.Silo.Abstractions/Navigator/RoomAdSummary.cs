using Orleans;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>A listed room's running promotion, as the navigator shows it beside the room.</summary>
[GenerateSerializer]
public sealed partial record RoomAdSummary(
    [property: Id(0)] string Name,
    [property: Id(1)] string Description,
    [property: Id(2)] int ExpiresInMinutes);
