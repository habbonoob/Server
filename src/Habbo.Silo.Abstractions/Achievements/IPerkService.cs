using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Achievements;

/// <summary>Resolves a player's feature allowances.</summary>
public interface IPerkService
{
    Task<IReadOnlyList<PerkAllowance>> GetAllowancesAsync(PlayerData player, CancellationToken ct = default);

    /// <summary>Whether the player has one perk, by its code; false for a perk the hotel does not know.</summary>
    Task<bool> IsAllowedAsync(PlayerData player, string code, CancellationToken ct = default);
}
