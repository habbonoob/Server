using System.Collections.Concurrent;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Habbo.Silo.Permissions;

/// <summary>Whether a player may do something: what their rank allows (the permissions table), unless they have an
/// exception of their own. The table and the exceptions are held in memory, read when the server starts and again on
/// <see cref="ReloadAsync"/>, so asking costs nothing. A permission the table does not have is nobody's.</summary>
public sealed class PermissionService(IDbContextFactory<AppDbContext> dbFactory, ILogger<PermissionService> logger)
    : IHostedService, Habbo.Silo.Abstractions.Hotel.IReloadable
{
    public string ReloadName => "permissions";

    Task Habbo.Silo.Abstractions.Hotel.IReloadable.ReloadAsync() => ReloadAsync();

    private PermissionSnapshot _snapshot = PermissionSnapshot.Empty;
    private readonly ConcurrentDictionary<string, bool> _unknownReported = new(StringComparer.Ordinal);

    public Task StartAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Reads the table and the exceptions again: after they were edited.</summary>
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var permissions = await db.Permissions.AsNoTracking().ToDictionaryAsync(p => p.Name, StringComparer.Ordinal, ct);
        var exceptions = (await db.UserPermissions.AsNoTracking().ToListAsync(ct))
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyDictionary<string, bool>)g.ToDictionary(e => e.PermissionName, e => e.Granted, StringComparer.Ordinal));
        _snapshot = new PermissionSnapshot(permissions, exceptions);
        _unknownReported.Clear();
        logger.LogInformation("Loaded {Permissions} permission(s), exceptions for {Players} player(s)", permissions.Count, exceptions.Count);
    }

    /// <summary>Whether the player, of this rank, may do it.</summary>
    public bool Has(Guid userId, SecurityLevel rank, string permission)
    {
        var snapshot = _snapshot;
        if (snapshot.Exceptions.TryGetValue(userId, out var own) && own.TryGetValue(permission, out var granted))
            return granted;

        if (snapshot.Permissions.TryGetValue(permission, out var row))
            return rank >= row.MinimumRank;

        if (_unknownReported.TryAdd(permission, true))
            logger.LogWarning("Permission {Permission} is not in the permissions table; nobody has it", permission);
        return false;
    }

    public bool Has(PlayerData player, string permission) => Has(player.Id, player.Rank, permission);

    public bool Has(User user, string permission) => Has(user.Id, user.Rank, permission);

    /// <summary>Whether the player may do it, their rank looked up.</summary>
    public async Task<bool> HasAsync(Guid userId, string permission)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rank = await db.Users.Where(u => u.Id == userId).Select(u => (SecurityLevel?)u.Rank).FirstOrDefaultAsync();
        return rank is { } level && Has(userId, level, permission);
    }

    /// <summary>What a permission allows, as the table describes it; empty when it has no description.</summary>
    public string DescriptionOf(string permission) =>
        _snapshot.Permissions.TryGetValue(permission, out var row) ? row.Description : string.Empty;

    /// <summary>The lowest rank a permission is given at, for those listing what they may do; null when it is nobody's.</summary>
    public SecurityLevel? MinimumRankOf(string permission) =>
        _snapshot.Permissions.TryGetValue(permission, out var row) ? row.MinimumRank : null;
}
