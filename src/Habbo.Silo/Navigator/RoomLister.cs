using Habbo.Silo.Users;
using Habbo.Domain.Rooms;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Habbo.Silo.Navigator;

/// <summary>Lists rooms for both navigators: runs a <see cref="RoomQuery"/> against the room store,
/// narrows it by the search box, and projects the rows with their live user counts, groups and
/// promotions. Rooms kept out of listings stay out, except for those who own or run them.</summary>
public sealed class RoomLister(IDbContextFactory<AppDbContext> dbFactory, IGrainFactory grains)
{
    /// <summary>The prefixes the search box understands, e.g. "owner:alice" or "tag:party".
    /// Anything without a prefix is matched against the room name and description.</summary>
    private const string OwnerPrefix = "owner:";
    private const string TagPrefix = "tag:";
    private const string GroupPrefix = "group:";
    private const string RoomNamePrefix = "roomname:";

    /// <param name="categoryId">Only rooms in this room category.</param>
    /// <param name="eventCategoryId">Only rooms promoted now in this event category.</param>
    /// <param name="roomIds">Only these rooms, in this order (an official folder's).</param>
    public async Task<List<RoomInfoData>> ListAsync(Guid requesterId, RoomQuery query, string filter, int maxResults,
        int? categoryId = null, int? eventCategoryId = null, IReadOnlyList<int>? roomIds = null)
    {
        var population = await PopulationAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var rooms = await QueryAsync(db, query, requesterId, filter, categoryId, eventCategoryId, roomIds, population, maxResults);
        return await ProjectAsync(db, rooms, requesterId, population);
    }

    public Task<Dictionary<int, int>> PopulationAsync() => grains.GetGrain<IRoomPopulationGrain>(0).GetAsync();

    /// <summary>The rooms as the navigator shows them to this requester.</summary>
    public async Task<List<RoomInfoData>> ProjectAsync(AppDbContext db, List<Room> rooms, Guid requesterId, Dictionary<int, int> population)
    {
        var roomIds = rooms.Select(room => room.Id).ToList();
        var groups = await db.Groups.AsNoTracking()
            .Where(g => roomIds.Contains(g.RoomId))
            .Select(g => new { g.RoomId, Group = new RoomGroup(g.Id, g.Name, g.BadgeCode),
                IsMember = db.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == requesterId) })
            .ToDictionaryAsync(g => g.RoomId);

        var now = DateTimeOffset.UtcNow;
        var ads = (await db.RoomEvents.AsNoTracking()
                .Where(e => roomIds.Contains(e.RoomId) && e.ExpiresAt > now)
                .ToListAsync())
            .GroupBy(e => e.RoomId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());

        return rooms.Select(room => RoomProjection.ToInfo(room, population.GetValueOrDefault(room.Id), requesterId,
                groups.GetValueOrDefault(room.Id)?.Group, groups.GetValueOrDefault(room.Id)?.IsMember ?? false,
                ads.GetValueOrDefault(room.Id) is { } ad
                    ? new RoomAdSummary(ad.Name, ad.Description, (int)Math.Ceiling((ad.ExpiresAt - now).TotalMinutes))
                    : null))
            .ToList();
    }

    private async Task<List<Room>> QueryAsync(
        AppDbContext db, RoomQuery kind, Guid requesterId, string filter, int? categoryId, int? eventCategoryId,
        IReadOnlyList<int>? roomIds, Dictionary<int, int> population, int maxResults)
    {
        var now = DateTimeOffset.UtcNow;
        var query = ApplyFilter(db, Listable(db, requesterId), filter);
        if (categoryId is { } category)
            query = query.Where(r => r.CategoryId == category);
        if (eventCategoryId is { } eventCategory)
            query = query.Where(r => db.RoomEvents.Any(e => e.RoomId == r.Id && e.ExpiresAt > now && e.CategoryId == eventCategory));
        if (roomIds is not null)
        {
            // a fixed list keeps its own order
            var listed = await query.Where(r => roomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
            return roomIds.Where(listed.ContainsKey).Select(id => listed[id]).Take(maxResults).ToList();
        }

        switch (kind)
        {
            case RoomQuery.Owned:
                query = query.Where(r => r.OwnerId == requesterId);
                break;
            case RoomQuery.StaffPicks:
                query = query.Where(r => r.IsStaffPick);
                break;
            case RoomQuery.Favourites:
                query = query.Where(r => db.RoomFavourites.Any(f => f.UserId == requesterId && f.RoomId == r.Id));
                break;
            case RoomQuery.FriendsRooms:
                query = query.Where(r => db.Friendships.Any(f => f.UserId == requesterId && f.FriendId == r.OwnerId));
                break;
            case RoomQuery.WhereFriendsAre:
                var friendRooms = await FriendRoomsAsync(db, requesterId);
                query = query.Where(r => friendRooms.Contains(r.Id));
                break;
            case RoomQuery.Rights:
                query = query.Where(r => db.RoomRights.Any(right => right.RoomId == r.Id && right.UserId == requesterId));
                break;
            case RoomQuery.GroupBases:
                query = query.Where(r => db.Groups.Any(g => g.RoomId == r.Id));
                break;
            case RoomQuery.MyGroupBases:
                query = query.Where(r => db.Groups.Any(g => g.RoomId == r.Id
                    && db.GroupMembers.Any(m => m.GroupId == g.Id && m.UserId == requesterId)));
                break;
            case RoomQuery.Promoted:
            case RoomQuery.NewlyPromoted:
                query = query.Where(r => db.RoomEvents.Any(e => e.RoomId == r.Id && e.ExpiresAt > now));
                break;
            case RoomQuery.History:
            case RoomQuery.FrequentHistory:
                return await HistoryAsync(db, query, requesterId, frequent: kind == RoomQuery.FrequentHistory, maxResults);
            case RoomQuery.Recommended:
                var categories = await db.RoomVisits
                    .Where(v => v.UserId == requesterId)
                    .Join(db.Rooms, v => v.RoomId, r => r.Id, (v, r) => r.CategoryId)
                    .GroupBy(c => c)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(3)
                    .ToListAsync();
                query = query.Where(r => r.OwnerId != requesterId && categories.Contains(r.CategoryId));
                break;
        }

        switch (kind)
        {
            case RoomQuery.HighestScore:
            case RoomQuery.Recommended:
                return await query.OrderByDescending(r => r.Score).ThenBy(r => r.Id).Take(maxResults).ToListAsync();
            case RoomQuery.NewlyPromoted:
                return await query
                    .OrderByDescending(r => db.RoomEvents.Where(e => e.RoomId == r.Id).Max(e => e.CreatedAt))
                    .Take(maxResults).ToListAsync();
        }

        var rooms = await query.OrderBy(r => r.Id).ToListAsync();

        // Busiest first where a listing is about where people are; population lives in memory.
        if (kind is RoomQuery.Popular or RoomQuery.GroupBases or RoomQuery.Promoted or RoomQuery.WhereFriendsAre or RoomQuery.Search)
            rooms = rooms.OrderByDescending(r => population.GetValueOrDefault(r.Id)).ThenBy(r => r.Id).ToList();

        return rooms.Take(maxResults).ToList();
    }

    /// <summary>The rooms anyone may see listed, and the hidden ones this requester runs.</summary>
    private static IQueryable<Room> Listable(AppDbContext db, Guid requesterId) =>
        db.Rooms.Include(r => r.Owner).AsNoTracking()
            .Where(r => (r.State != RoomState.Invisible && !r.HiddenByBuildersClub)
                || r.OwnerId == requesterId
                || db.RoomRights.Any(right => right.RoomId == r.Id && right.UserId == requesterId));

    /// <summary>The rooms the requester's online friends are in, among those who let friends follow and let the
    /// requester see them online.</summary>
    private async Task<HashSet<int>> FriendRoomsAsync(AppDbContext db, Guid requesterId)
    {
        var friends = await db.Friendships
            .Where(f => f.UserId == requesterId && !f.Friend!.FollowingBlocked)
            .Select(f => f.FriendId)
            .ToListAsync();
        var online = (await grains.GetGrain<IUserPresenceGrain>(IUserPresenceGrain.Key).WhichAreOnlineAsync(friends))
            .Except(await OnlineVisibility.HiddenFromAsync(db, requesterId, friends));

        var rooms = new HashSet<int>();
        foreach (var friend in online)
            if (await grains.GetGrain<IPlayerGrain>(friend).GetCurrentRoomAsync() is { } roomId)
                rooms.Add(roomId);
        return rooms;
    }

    /// <summary>The rooms the requester went into: the latest first, or the most visited first.</summary>
    private static async Task<List<Room>> HistoryAsync(AppDbContext db, IQueryable<Room> listable, Guid requesterId, bool frequent, int maxResults)
    {
        var visits = db.RoomVisits
            .Where(v => v.UserId == requesterId)
            .GroupBy(v => v.RoomId)
            .Select(g => new { RoomId = g.Key, Last = g.Max(v => v.Id), Count = g.Count() });
        var ordered = frequent
            ? visits.OrderByDescending(v => v.Count).ThenByDescending(v => v.Last)
            : visits.OrderByDescending(v => v.Last);
        var roomIds = await ordered.Take(maxResults).Select(v => v.RoomId).ToListAsync();

        var rooms = await listable.Where(r => roomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
        return roomIds.Where(rooms.ContainsKey).Select(id => rooms[id]).ToList();
    }

    /// <summary>Applies the search box text: a prefixed term narrows to one field, plain text
    /// matches the name or description.</summary>
    private static IQueryable<Room> ApplyFilter(AppDbContext db, IQueryable<Room> query, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return query;

        var term = filter.Trim();

        if (Unprefixed(term, OwnerPrefix) is { } owner)
            return query.Where(r => r.Owner != null && EF.Functions.ILike(r.Owner.Username, $"%{owner}%"));

        if (Unprefixed(term, TagPrefix) is { } tag)
            return query.Where(r => r.Tags.Contains(tag));

        if (Unprefixed(term, RoomNamePrefix) is { } name)
            return query.Where(r => EF.Functions.ILike(r.Name, $"%{name}%"));

        if (Unprefixed(term, GroupPrefix) is { } group)
            return query.Where(r => db.Groups.Any(g => g.RoomId == r.Id && EF.Functions.ILike(g.Name, $"%{group}%")));

        var pattern = $"%{term}%";
        return query.Where(r => EF.Functions.ILike(r.Name, pattern) || EF.Functions.ILike(r.Description, pattern));
    }

    private static string? Unprefixed(string term, string prefix) =>
        term.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? term[prefix.Length..].Trim() : null;
}
