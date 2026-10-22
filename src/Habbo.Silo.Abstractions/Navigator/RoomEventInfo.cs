using Orleans;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>A room's running promotion (event), for the room's event bar.</summary>
[GenerateSerializer]
public sealed partial record RoomEventInfo(
    [property: Id(0)] int Id,
    [property: Id(1)] int OwnerAccountId,
    [property: Id(2)] string OwnerName,
    [property: Id(3)] int RoomId,
    [property: Id(4)] int Type,
    [property: Id(5)] string Name,
    [property: Id(6)] string Description,
    [property: Id(7)] DateTimeOffset CreatedAt,
    [property: Id(8)] DateTimeOffset ExpiresAt,
    [property: Id(9)] int CategoryId);
