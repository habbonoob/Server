using Orleans;

namespace Habbo.Silo.Abstractions.Games;

/// <summary>A game about to start: its id, the arena and who plays.</summary>
[GenerateSerializer]
public sealed partial record SnowWarGameSetup(
    [property: Id(0)] int GameId,
    [property: Id(1)] int FieldType,
    [property: Id(2)] List<SnowWarPlayer> Players);
