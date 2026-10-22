namespace Habbo.Silo.Abstractions.Furni;

/// <summary>One key/value pair of a map-shaped item.</summary>
[GenerateSerializer]
public sealed partial record FurniMapEntryView(
    [property: Id(0)] string Key,
    [property: Id(1)] string Value);
