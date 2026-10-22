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

        return perks
            .Select(perk => new PerkAllowance(perk.Code, perk.ErrorMessage, player.Rank >= perk.MinimumRank))
            .ToList();
    }

    public async Task<bool> IsAllowedAsync(PlayerData player, string code, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Perks.AnyAsync(perk => perk.Code == code && perk.MinimumRank <= player.Rank, ct);
    }
}
