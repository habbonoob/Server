using Habbo.Domain.Users;
using Habbo.Protocol.Messages.Outgoing.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Muting and blocking other users.</summary>
public sealed partial class PlayerGrain
{
    public async Task SetIgnoredAsync(int accountId, bool ignored)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (target is null || target.Id == PlayerId)
            return;

        var row = await db.IgnoredUsers.FirstOrDefaultAsync(i => i.UserId == PlayerId && i.IgnoredUserId == target.Id);
        if (!ignored)
        {
            if (row is not null)
            {
                db.IgnoredUsers.Remove(row);
                await db.SaveChangesAsync();
            }

            Send(new IgnoreResultMessageComposer(IgnoreResults.Unignored, accountId));
            return;
        }

        if (row is not null)
        {
            Send(new IgnoreResultMessageComposer(IgnoreResults.Ignored, accountId));
            return;
        }

        var list = await db.IgnoredUsers.Where(i => i.UserId == PlayerId).OrderBy(i => i.CreatedAt).ToListAsync();
        var dropOldest = list.Count >= accountOptions.Value.IgnoreListLimit;
        if (dropOldest)
            db.IgnoredUsers.Remove(list[0]);

        db.IgnoredUsers.Add(new IgnoredUser { UserId = PlayerId, IgnoredUserId = target.Id });
        await db.SaveChangesAsync();
        Send(new IgnoreResultMessageComposer(dropOldest ? IgnoreResults.IgnoredOldestDropped : IgnoreResults.Ignored, accountId));
    }

    public async Task SetBlockedAsync(int accountId, bool blocked)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccountId == accountId);
        if (target is null || target.Id == PlayerId)
            return;

        var row = await db.BlockedUsers.FirstOrDefaultAsync(b => b.UserId == PlayerId && b.BlockedUserId == target.Id);
        if (blocked && row is null)
            db.BlockedUsers.Add(new BlockedUser { UserId = PlayerId, BlockedUserId = target.Id });
        else if (!blocked && row is not null)
            db.BlockedUsers.Remove(row);

        await db.SaveChangesAsync();
        Send(new BlockUserUpdateMessageComposer(blocked ? BlockResults.Blocked : BlockResults.Unblocked, accountId));
    }
}
