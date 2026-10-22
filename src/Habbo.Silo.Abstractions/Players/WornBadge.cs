using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>A badge in one of the five wearing slots, with how many own it and how rare that makes it (as the
/// hotel tells them; not what the client sends when it changes what it wears).</summary>
[GenerateSerializer]
public sealed partial record WornBadge(
    [property: Id(0)] int Slot,
    [property: Id(1)] string Code,
    [property: Id(2)] int Owners = 0,
    [property: Id(3)] BadgeRarity Rarity = BadgeRarity.Common);
