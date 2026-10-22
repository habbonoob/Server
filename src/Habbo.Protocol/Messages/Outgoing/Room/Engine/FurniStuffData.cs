using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>An item's data, as every furniture packet carries it.
///
/// The header is one int: the layout in the low byte, flags in the high byte. The client picks
/// a parser from the low byte, so the payload that follows depends entirely on it — which is
/// why the fields below are conditional rather than a union. They are declared in wire order,
/// and because no two layouts share a field, each layout reads back exactly its own.
///
/// The trailer is the odd one out: every layout reads it except HighScore, which is the only
/// one that does not chain to its base class. Writing it for a limited-edition scoreboard would
/// desync the packet.</summary>
[WireStruct]
public sealed partial record FurniStuffData(
    int TypeAndFlags,
    [property: WireWhen("HasState")] string? State,
    [property: WireWhen("IsMap")] IReadOnlyList<FurniMapEntryData>? MapEntries,
    [property: WireWhen("IsStringArray")] IReadOnlyList<string>? StringValues,
    [property: WireWhen("IsVoteResult")] int? VoteResult,
    [property: WireWhen("IsIntArray")] IReadOnlyList<int>? IntValues,
    [property: WireWhen("IsHighScore")] int? ScoreType,
    [property: WireWhen("IsHighScore")] int? ClearType,
    [property: WireWhen("IsHighScore")] IReadOnlyList<FurniHighScoreEntryData>? HighScoreEntries,
    [property: WireWhen("IsCrackable")] int? Hits,
    [property: WireWhen("IsCrackable")] int? Target,
    [property: WireWhen("HasUniqueEdition")] int? UniqueSerialNumber,
    [property: WireWhen("HasUniqueEdition")] int? UniqueSeriesSize)
{
    /// <summary>The client masks the header: layout in the low byte, flags in the high.</summary>
    private const int TypeMask = 0xFF;
    private const int FlagMask = 0xFF00;

    /// <summary>The only flag the client reads — the item is one of a numbered run.</summary>
    public const int UniqueSerialNumberFlag = 0x0100;

    public const int LegacyType = 0;
    public const int MapType = 1;
    public const int StringArrayType = 2;
    public const int VoteResultType = 3;
    public const int EmptyType = 4;
    public const int IntArrayType = 5;
    public const int HighScoreType = 6;
    public const int CrackableType = 7;

    public int Type => TypeAndFlags & TypeMask;

    /// <summary>The layouts that begin with a string. For ordinary furniture it is the visual
    /// state the client renders.</summary>
    public bool HasState =>
        Type is LegacyType or VoteResultType or HighScoreType or CrackableType;

    public bool IsMap => Type == MapType;
    public bool IsStringArray => Type == StringArrayType;
    public bool IsVoteResult => Type == VoteResultType;
    public bool IsIntArray => Type == IntArrayType;
    public bool IsHighScore => Type == HighScoreType;
    public bool IsCrackable => Type == CrackableType;

    /// <summary>HighScore is excluded deliberately: it is the one layout that never reads the
    /// trailer, so writing it there would shift everything after this item in the packet.</summary>
    public bool HasUniqueEdition =>
        (TypeAndFlags & FlagMask & UniqueSerialNumberFlag) != 0 && !IsHighScore;

    /// <summary>The header for a layout, with the unique-edition flag when the item is one.</summary>
    public static int HeaderFor(int type, bool uniqueEdition) =>
        type | (uniqueEdition ? UniqueSerialNumberFlag : 0);

    /// <summary>The wire form of an item's data. Every field the layout declares is filled —
    /// with a default where the view has nothing — because a null under a true condition
    /// would throw rather than let the packet desync.</summary>
    public static FurniStuffData From(FurniDataView data)
    {
        var uniqueEdition = data.UniqueSerialNumber is not null;

        return new FurniStuffData(
            HeaderFor(data.Type, uniqueEdition),
            data.State ?? "",
            data.MapEntries?.Select(e => new FurniMapEntryData(e.Key, e.Value)).ToList() ?? [],
            data.StringValues ?? [],
            data.VoteResult ?? 0,
            data.IntValues ?? [],
            data.ScoreType ?? 0,
            data.ClearType ?? 0,
            data.HighScoreEntries?.Select(e => new FurniHighScoreEntryData(e.Score, e.Users)).ToList() ?? [],
            data.Hits ?? 0,
            data.Target ?? 0,
            data.UniqueSerialNumber ?? 0,
            data.UniqueSeriesSize ?? 0);
    }
}
