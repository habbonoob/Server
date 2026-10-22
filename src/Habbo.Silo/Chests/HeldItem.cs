namespace Habbo.Silo.Chests;

/// <summary>An item a chest holds, as far as the room needs to know it without asking the database.</summary>
public sealed record HeldItem(int ItemId, ChestKind Kind, DateTimeOffset StoredAt, int DrawOrder);
