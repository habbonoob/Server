using Orleans;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>A client's performance report: how long it has run, on what, and how smoothly.</summary>
[GenerateSerializer]
public sealed partial record ClientPerformance(
    [property: Id(0)] int ElapsedSeconds,
    [property: Id(1)] string UserAgent,
    [property: Id(2)] string FlashVersion,
    [property: Id(3)] string OperatingSystem,
    [property: Id(4)] string Browser,
    [property: Id(5)] bool IsDebugger,
    [property: Id(6)] int MemoryKb,
    [property: Id(7)] int GarbageCollections,
    [property: Id(8)] int SlowUpdates,
    [property: Id(9)] int AverageUpdateInterval,
    [property: Id(10)] int LagSpikes);
