namespace Habbo.Domain.Rooms.Copies;

/// <summary>What a copied floor item shows, in the client's own layouts (<see cref="Furniture.Data.FurniDataType"/>):
/// <see cref="Type"/> says which, and only that layout's fields are set.</summary>
/// <param name="State">The single string of the plain layout (and the vote and crackable layouts' own).</param>
/// <param name="Map">The key/value layout (a mannequin's figure, a background's colours).</param>
/// <param name="Strings">The string-array layout.</param>
/// <param name="Ints">The int-array layout.</param>
public sealed record RoomCopyItemData(
    int Type,
    string? State,
    IReadOnlyDictionary<string, string>? Map,
    IReadOnlyList<string>? Strings,
    IReadOnlyList<int>? Ints,
    int? VoteResult,
    int? ScoreType,
    int? ClearType,
    IReadOnlyList<RoomCopyHighScore>? HighScores,
    int? Hits,
    int? Target);
