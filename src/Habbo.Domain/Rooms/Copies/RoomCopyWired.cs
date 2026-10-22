namespace Habbo.Domain.Rooms.Copies;

/// <summary>A wired box's settings as its dialog showed them on the hotel it was copied from. The furni it picks
/// (<see cref="StuffIds"/>, <see cref="StuffIds2"/>) are by their ids there (<see cref="RoomCopyFloorItem.SourceId"/>),
/// and the variables it uses by their ids there (<see cref="RoomCopy.Variables"/>); the import gives both their new
/// ids.</summary>
/// <param name="DelayInPulses">An effect's delay, in half seconds.</param>
/// <param name="Quantifier">A condition's quantifier (all or any of the picked furni).</param>
/// <param name="IsFilter">A selector's filter mode.</param>
/// <param name="IsInvert">A condition's or selector's negation.</param>
public sealed record RoomCopyWired(
    IReadOnlyList<long> StuffIds,
    IReadOnlyList<long> StuffIds2,
    string StringParam,
    IReadOnlyList<int> IntParams,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<int> FurniSources,
    IReadOnlyList<int> UserSources,
    int DelayInPulses,
    int Quantifier,
    bool IsFilter,
    bool IsInvert);
