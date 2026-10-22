namespace Habbo.Domain.Rooms.Copies;

/// <summary>A line of a copied high-score board.</summary>
public sealed record RoomCopyHighScore(int Score, IReadOnlyList<string> Users);
