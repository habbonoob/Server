using Microsoft.EntityFrameworkCore;

namespace Habbo.Infrastructure;

/// <summary>A lock on one thing a player does, held by the database for the rest of the open transaction: two
/// requests by the same player (two sessions, or one still running on a session a second login replaced) take
/// their turns at it, wherever they run.</summary>
public static class PlayerLock
{
    /// <summary>Waits for the lock on <paramref name="scope"/> for the player; it is let go when the transaction ends.</summary>
    public static Task TakeAsync(AppDbContext db, Guid playerId, string scope, CancellationToken ct = default) =>
        db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({Key(playerId, scope)})", ct);

    /// <summary>The lock's number: the player's id folded with the scope's name. Two that collide only wait on each other.</summary>
    private static long Key(Guid playerId, string scope)
    {
        var bytes = playerId.ToByteArray();
        var hash = BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
        foreach (var c in scope)
            hash = hash * 31 + c;
        return hash;
    }
}
