using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Achievements;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Perks;

/// <summary>Resolves a player's feature allowances from the perks table. A perk is allowed
/// when the player's rank reaches its minimum, so the catalog and who gets what are both data.</summary>
public sealed class PerkService(IDbContextFactory<AppDbContext> dbFactory) : IPerkService
{
    public async Task<IReadOnlyList<PerkAllowance>> GetAllowancesAsync(PlayerData player, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var perks = await db.Perks.AsNoTracking().OrderBy(p => p.Code).ToListAsync(ct);

        return perks.Select(perk => Allowance(perk, player.Rank)).ToList();
    }

    /// <summary>One perk for a player of <paramref name="rank"/>. The perk's message is the requirement the player has not
    /// met, so it goes with a denied perk only: habbo.com (4 November 2026) sends an allowed perk with an empty message
    /// and a denied one with its requirement (USE_GUIDE_TOOL: requirement.unfulfilled.helper_level_4).</summary>
    public static PerkAllowance Allowance(Perk perk, SecurityLevel rank)
    {
        var allowed = rank >= perk.MinimumRank;
        return new PerkAllowance(perk.Code, allowed ? string.Empty : perk.ErrorMessage, allowed);
    }

    public async Task<bool> IsAllowedAsync(PlayerData player, string code, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Perks.AnyAsync(perk => perk.Code == code && perk.MinimumRank <= player.Rank, ct);
    }
}
