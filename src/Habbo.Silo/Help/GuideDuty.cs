using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Help;

/// <summary>A guide on duty: which requests they take, and since when.</summary>
public sealed record GuideDuty(PlayerData Guide, bool Tours, bool Helps, bool ChatReviews, DateTimeOffset Since);
