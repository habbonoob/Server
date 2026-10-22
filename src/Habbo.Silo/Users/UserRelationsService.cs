using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Users;

/// <summary>Reads the lists of people a player has shut out. They are read all over the client —
/// room chat, the console, friend requests — so they belong to the account rather than to the
/// friend list or the console.</summary>
public sealed class UserRelationsService(IDbContextFactory<AppDbContext> dbFactory) : IUserRelationsService
{
    public async Task<IReadOnlyList<int>> GetIgnoredAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.IgnoredUsers
            .AsNoTracking()
            .Where(ignored => ignored.UserId == playerId)
            .Select(ignored => ignored.Ignored!.AccountId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<int>> GetBlockedAsync(Guid playerId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BlockedUsers
            .AsNoTracking()
            .Where(blocked => blocked.UserId == playerId)
            .Select(blocked => blocked.Blocked!.AccountId)
            .ToListAsync(ct);
    }
}
