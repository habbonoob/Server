using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Inventory.Badges;
using Habbo.Protocol.Messages.Outgoing.Notifications;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Grains.Players;

/// <summary>Badges: the inventory, what the player wears, and badges handed over by request code.</summary>
public sealed partial class PlayerGrain
{
    /// <summary>Achievement badge codes are the achievement's name with its level after it; the
    /// point-limits packet sends the name without this prefix, and the client puts it back.</summary>
    private const string AchievementPrefix = "ACH_";

    public async Task SendBadgesAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await db.UserBadges.AsNoTracking()
            .Where(b => b.UserId == PlayerId)
            .OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.Code })
            .ToListAsync();
        var owners = await BadgeLeaderboards.OwnersAsync(db, owned.Select(b => b.Code).ToList());
        var badges = owned.Select(b =>
        {
            var (shown, rarity) = badgeLeaderboards.Describe(owners.GetValueOrDefault(b.Code));
            return new BadgeData(b.Id, b.Code, shown, rarity);
        }).ToList();

        // an empty inventory is still one fragment, or the client waits for it forever
        var fragments = badges.Chunk(Math.Max(1, accountOptions.Value.BadgeFragmentSize)).ToList();
        if (fragments.Count == 0)
            fragments.Add([]);

        for (var i = 0; i < fragments.Count; i++)
            Send(new BadgesComposer(fragments.Count, i, fragments[i]));
    }

    public async Task<IReadOnlyList<WornBadge>> SetWornBadgesAsync(IReadOnlyList<WornBadge> slots)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var badges = await db.UserBadges.Where(b => b.UserId == PlayerId).ToListAsync();
        foreach (var badge in badges)
            badge.Slot = UserBadge.NotWorn;

        var usedSlots = new HashSet<int>();
        foreach (var (slot, code) in slots.Select(s => (s.Slot, s.Code)))
        {
            if (slot is < 1 or > UserBadge.WornSlots || string.IsNullOrEmpty(code) || !usedSlots.Add(slot))
                continue;

            // a badge worn twice keeps its first slot
            if (badges.FirstOrDefault(b => b.Code == code && b.Slot == UserBadge.NotWorn) is { } badge)
                badge.Slot = slot;
        }

        await db.SaveChangesAsync();
        return await WornAsync(db, badges);
    }

    public async Task<IReadOnlyList<WornBadge>> GetWornBadgesAsync(int accountId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var badges = await db.UserBadges.AsNoTracking()
            .Where(b => b.User!.AccountId == accountId && b.Slot != UserBadge.NotWorn)
            .ToListAsync();
        return await WornAsync(db, badges);
    }

    /// <summary>The badges worn, in slot order, each with how many own it and how rare that makes it.</summary>
    private async Task<List<WornBadge>> WornAsync(AppDbContext db, IEnumerable<UserBadge> badges)
    {
        var worn = badges.Where(b => b.Slot != UserBadge.NotWorn).OrderBy(b => b.Slot).ToList();
        var owners = await BadgeLeaderboards.OwnersAsync(db, worn.Select(b => b.Code).ToList());
        return worn.Select(b =>
        {
            var (shown, rarity) = badgeLeaderboards.Describe(owners.GetValueOrDefault(b.Code));
            return new WornBadge(b.Slot, b.Code, shown, rarity);
        }).ToList();
    }

    /// <summary>How many own a badge, as the hotel tells it, and how rare that makes it.</summary>
    private async Task<(int Owners, BadgeRarity Rarity)> DescribeBadgeAsync(AppDbContext db, string code) =>
        badgeLeaderboards.Describe((await BadgeLeaderboards.OwnersAsync(db, [code])).GetValueOrDefault(code));

    /// <summary>Tells the player a badge landed in their inventory, with its rarity.</summary>
    private async Task SendBadgeReceivedAsync(AppDbContext db, int badgeId, string code)
    {
        var (owners, rarity) = await DescribeBadgeAsync(db, code);
        Send(new BadgeReceivedComposer(badgeId, code, owners, rarity));
    }

    public async Task SendBadgeInfoAsync(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length > UserBadge.CodeMaxLength)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var (owners, rarity) = await DescribeBadgeAsync(db, code);
        var ownId = await db.UserBadges.Where(b => b.UserId == PlayerId && b.Code == code).Select(b => b.Id).FirstOrDefaultAsync();
        Send(new BadgeInfoComposer(ownId, code, owners, rarity));
    }

    public async Task SendBadgeLeaderboardAsync(int type, int rarity, int page, int pageSize)
    {
        var board = await badgeLeaderboards.PageAsync(PlayerId, type, rarity, page, pageSize);
        static BadgeLeaderboardEntryData Wire(Habbo.Silo.Users.BadgeLeaderboardEntry e) => new(e.AccountId, e.Username, e.Figure, e.Rank, e.Score);
        Send(new BadgeLeaderboardResultMessageComposer(type, rarity, page, pageSize, board.TotalEntries,
            board.Entries.Select(Wire).ToList(), Wire(board.Own)));
    }

    public async Task<bool> GiveBadgeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > UserBadge.CodeMaxLength)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.UserBadges.AnyAsync(b => b.UserId == PlayerId && b.Code == code))
            return false;

        var badge = new UserBadge { UserId = PlayerId, Code = code };
        db.UserBadges.Add(badge);
        await db.SaveChangesAsync();

        await SendBadgeReceivedAsync(db, badge.Id, badge.Code);
        Send(UnseenItemsComposer.Badges([badge.Id]));
        return true;
    }

    public async Task<bool> RemoveBadgeAsync(string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var badge = await db.UserBadges.FirstOrDefaultAsync(b => b.UserId == PlayerId && b.Code == code);
        if (badge is null)
            return false;

        db.UserBadges.Remove(badge);
        await db.SaveChangesAsync();
        await SendBadgesAsync();
        return true;
    }

    public async Task<bool> HasBadgeAsync(string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.UserBadges.AnyAsync(b => b.UserId == PlayerId && b.Code == code);
    }

    public async Task SendBadgePointLimitsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var achievements = await db.Achievements.AsNoTracking()
            .Include(a => a.Levels)
            .OrderBy(a => a.Id)
            .ToListAsync();

        Send(new BadgePointLimitsComposer(achievements
            .Where(a => a.Levels.Count > 0)
            .Select(a => new BadgePointLimitData(
                a.Name.StartsWith(AchievementPrefix, StringComparison.Ordinal) ? a.Name[AchievementPrefix.Length..] : a.Name,
                a.Levels.OrderBy(l => l.Level).Select(l => new BadgeLevelLimitData(l.Level, l.RequiredProgress)).ToList()))
            .ToList()));
    }

    public async Task RequestBadgeAsync(string requestCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var request = await db.BadgeRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Code == requestCode);
        if (request is not null)
            await GiveBadgeAsync(request.BadgeCode);

        await SendBadgeRequestStatusAsync(requestCode);
    }

    public async Task SendBadgeRequestStatusAsync(string requestCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var badgeCode = await db.BadgeRequests.AsNoTracking()
            .Where(r => r.Code == requestCode)
            .Select(r => r.BadgeCode)
            .FirstOrDefaultAsync();

        var fulfilled = badgeCode is not null
                        && await db.UserBadges.AnyAsync(b => b.UserId == PlayerId && b.Code == badgeCode);
        Send(new IsBadgeRequestFulfilledComposer(requestCode, fulfilled));
    }
}
