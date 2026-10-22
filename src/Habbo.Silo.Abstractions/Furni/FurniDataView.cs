namespace Habbo.Silo.Abstractions.Furni;

/// <summary>An item's data as it crosses from the silo to the gateway: the layout, then only
/// the fields that layout carries. Mirrors the furni_* tables rather than any one packet, so
/// the same view feeds the inventory, the room and anything added later.</summary>
[GenerateSerializer]
public sealed partial record FurniDataView(
    [property: Id(0)] int Type,
    [property: Id(1)] string? State,
    [property: Id(2)] List<FurniMapEntryView>? MapEntries,
    [property: Id(3)] List<string>? StringValues,
    [property: Id(4)] int? VoteResult,
    [property: Id(5)] List<int>? IntValues,
    [property: Id(6)] int? ScoreType,
    [property: Id(7)] int? ClearType,
    [property: Id(8)] List<FurniHighScoreEntryView>? HighScoreEntries,
    [property: Id(9)] int? Hits,
    [property: Id(10)] int? Target,
    [property: Id(11)] int? UniqueSerialNumber,
    [property: Id(12)] int? UniqueSeriesSize)
{
    /// <summary>The client's number for the single-string layout.</summary>
    public const int LegacyType = 0;

    /// <summary>What an item with nothing recorded looks like: the plain layout, no state.</summary>
    public static FurniDataView Plain { get; } =
        new(LegacyType, "", null, null, null, null, null, null, null, null, null, null, null);

    /// <summary>The single-string layout, which is all ordinary furniture uses.</summary>
    public static FurniDataView WithState(string state) => Plain with { State = state };

    /// <summary>True when the item carries nothing of its own — no state, no payload, not one
    /// of a numbered run. Only such items may share an inventory tile, because the client
    /// stacks by type and would hide whatever made two of them different.</summary>
    public bool IsPlain =>
        Type == LegacyType && string.IsNullOrEmpty(State) && UniqueSerialNumber is null;

    /// <summary>The string the client treats as the item's state, for the places that carry
    /// a bare string rather than full data — wall items among them.</summary>
    public string StateText => State ?? "";
}
