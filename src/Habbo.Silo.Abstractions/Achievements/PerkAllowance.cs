using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Achievements;

/// <summary>A feature allowance sent in PerkAllowancesMessageComposer (3291). The client gates
/// features on these — most importantly the new navigator and the free-flow chat init.</summary>
[GenerateSerializer]
public partial record PerkAllowance(
    [property: Id(0)] string Code,
    [property: Id(1)] string ErrorMessage,
    [property: Id(2)] bool IsAllowed);
