using Habbo.Silo.Abstractions.Permissions;
using System.Globalization;
using Habbo.Domain.Navigator;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Navigator;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Achievements;
using Habbo.Silo.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Habbo.Silo.Permissions;
using Habbo.Silo.Users;
using Habbo.Silo.Collections;

namespace Habbo.Silo.Navigator;

/// <summary>The old navigator and what surrounds finding a room.</summary>
public sealed class RoomDirectoryService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    PlayerMessenger messenger,
    PermissionService permissions,
    RoomLister lister,
    INavigatorCatalog catalog,
    IOptions<NavigatorOptions> options,
    IOptions<RoomOptions> roomOptions) : IRoomDirectoryService
{
    /// <summary>Ad statistics are kept once per player, kind and room a minute: each is a packet the client sends on a
    /// click or a tab change, and a row for every one let a client write rows as fast as it could send.</summary>
    private readonly RecentlySeen<(Guid, RoomAdInteractionKind, int?)> _recentInteractions = new(TimeSpan.FromMinutes(1));

    /// <summary>The forward the client links to from its "find new friends" button: any busy room.</summary>
    private const string RandomFriendingRoom = "random_friending_room";

    /// <summary>What the popular search sends for "every category".</summary>
    private const int AllCategories = -1;

    public async Task SearchAsync(PlayerData player, LegacyRoomSearch search, string param, int? adIndex)
    {
        var (query, filter, categoryId) = search switch
        {
            LegacyRoomSearch.Popular => (RoomQuery.Popular, string.Empty,
                int.TryParse(param, CultureInfo.InvariantCulture, out var category) && category != AllCategories ? category : (int?)null),
            LegacyRoomSearch.HighestScore => (RoomQuery.HighestScore, string.Empty, null),
            LegacyRoomSearch.MyFriendsRooms => (RoomQuery.FriendsRooms, string.Empty, null),
            LegacyRoomSearch.RoomsWhereMyFriendsAre => (RoomQuery.WhereFriendsAre, string.Empty, null),
            LegacyRoomSearch.MyRooms => (RoomQuery.Owned, string.Empty, null),
            LegacyRoomSearch.MyFavourites => (RoomQuery.Favourites, string.Empty, null),
            LegacyRoomSearch.MyHistory => (RoomQuery.History, string.Empty, null),
            LegacyRoomSearch.GuildBases => (RoomQuery.GroupBases, string.Empty, null),
            LegacyRoomSearch.TopPromotions => (RoomQuery.Promoted, string.Empty, null),
            LegacyRoomSearch.NewPromotions => (RoomQuery.NewlyPromoted, string.Empty, null),
            LegacyRoomSearch.MyRoomRights => (RoomQuery.Rights, string.Empty, null),
            LegacyRoomSearch.MyGuildBases => (RoomQuery.MyGroupBases, string.Empty, null),
            LegacyRoomSearch.MyRecommended => (RoomQuery.Recommended, string.Empty, null),
            LegacyRoomSearch.MyFrequentHistory => (RoomQuery.FrequentHistory, string.Empty, null),
            // the text searches carry their own prefix ("tag:", "owner:"...), which the lister reads
            _ => (RoomQuery.Popular, param, (int?)null),
        };

        var rooms = await lister.ListAsync(player.Id, query, filter, options.Value.SearchResultLimit, categoryId);
        await SendResultAsync(player, search, param, rooms, adIndex);
    }

    public async Task SearchCompetitionAsync(PlayerData player, int goalId, int pageIndex)
    {
        var pageSize = options.Value.CompetitionPageSize;
        var population = await lister.PopulationAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var entries = db.CompetitionRoomEntries.Where(e => e.GoalId == goalId);
        var pageCount = (int)Math.Ceiling(await entries.CountAsync() / (double)pageSize);
        var page = Math.Clamp(pageIndex, 0, Math.Max(0, pageCount - 1));
        var roomIds = await entries.OrderByDescending(e => e.Votes).ThenBy(e => e.SubmittedAt)
            .Skip(page * pageSize).Take(pageSize).Select(e => e.RoomId).ToListAsync();
        var rooms = await db.Rooms.Include(r => r.Owner).AsNoTracking().Where(r => roomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);

        await messenger.SendAsync(player.Id, new CompetitionRoomsDataMessageComposer(goalId, page, pageCount));
        var listed = await lister.ProjectAsync(db, roomIds.Where(rooms.ContainsKey).Select(id => rooms[id]).ToList(), player.Id, population);
        await SendResultAsync(player, LegacyRoomSearch.Competition, string.Empty, listed, adIndex: null);
    }

    /// <summary>The results, with one of the ad entries in the ad slot when the search shows one.</summary>
    private async Task SendResultAsync(PlayerData player, LegacyRoomSearch search, string param, List<RoomInfoData> rooms, int? adIndex)
    {
        var ad = adIndex is { } index ? await AdEntryAsync(player, index) : null;
        await messenger.SendAsync(player.Id, new GuestRoomSearchResultComposer(
            (int)search, param, rooms.Select(GuestRoomData.From).ToList(), ad is not null, ad));
    }

    private async Task<OfficialRoomEntryData?> AdEntryAsync(PlayerData player, int adIndex)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ads = await db.OfficialRoomEntries.AsNoTracking().Where(e => e.IsAd && e.FolderId == null)
            .OrderBy(e => e.SortOrder).ToListAsync();
        if (ads.Count == 0)
            return null;

        var entry = ads[Math.Abs(adIndex) % ads.Count];
        return (await ToEntryDataAsync(db, player, [entry], ads)).Single();
    }

    public async Task SendOfficialRoomsAsync(PlayerData player, int adIndex)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var all = await db.OfficialRoomEntries.AsNoTracking().OrderBy(e => e.SortOrder).ToListAsync();
        var listed = all.Where(e => !e.IsAd).ToList();
        var ad = await AdEntryAsync(player, adIndex);

        await messenger.SendAsync(player.Id, new OfficialRoomsComposer(
            await ToEntryDataAsync(db, player, listed, listed),
            ad is null ? [] : [ad],
            await PromotedCategoriesAsync(db, player)));
    }

    /// <summary>The entries as the client reads them: rooms with their details, folders counting the
    /// users of everything inside them, tags the users of the rooms that carry them.</summary>
    private async Task<List<OfficialRoomEntryData>> ToEntryDataAsync(
        AppDbContext db, PlayerData player, List<OfficialRoomEntry> entries, List<OfficialRoomEntry> tree)
    {
        var population = await lister.PopulationAsync();
        var roomIds = tree.Where(e => e.RoomId is not null).Select(e => e.RoomId!.Value).ToList();
        var rooms = await db.Rooms.Include(r => r.Owner).AsNoTracking().Where(r => roomIds.Contains(r.Id)).ToListAsync();
        var projected = (await lister.ProjectAsync(db, rooms, player.Id, population)).ToDictionary(r => r.RoomId);

        var tags = tree.Where(e => e.Type == OfficialRoomEntryType.Tag && e.Tag is not null).Select(e => e.Tag!).Distinct().ToList();
        var tagged = await db.Rooms.AsNoTracking().Where(r => r.Tags.Any(t => tags.Contains(t)))
            .Select(r => new { r.Id, r.Tags }).ToListAsync();

        int UserCount(OfficialRoomEntry entry) => entry.Type switch
        {
            OfficialRoomEntryType.Room => entry.RoomId is { } id ? population.GetValueOrDefault(id) : 0,
            OfficialRoomEntryType.Tag => tagged.Where(r => r.Tags.Contains(entry.Tag!)).Sum(r => population.GetValueOrDefault(r.Id)),
            _ => tree.Where(child => child.FolderId == entry.Id).Sum(UserCount),
        };

        return entries.Select(entry => new OfficialRoomEntryData(
                entry.Id,
                entry.PopupCaption,
                entry.PopupDescription,
                entry.ShowDetails ? OfficialRoomEntryData.DetailsShown : OfficialRoomEntryData.DetailsHidden,
                entry.PicText,
                entry.PicRef,
                entry.FolderId ?? OfficialRoomEntryData.NoFolder,
                UserCount(entry),
                (int)entry.Type,
                entry.Type == OfficialRoomEntryType.Tag ? entry.Tag ?? string.Empty : null,
                entry.Type == OfficialRoomEntryType.Room && entry.RoomId is { } roomId && projected.TryGetValue(roomId, out var room)
                    ? GuestRoomData.From(room)
                    : null,
                entry.Type == OfficialRoomEntryType.Folder ? entry.Open : null))
            // a room entry whose room is gone has nothing to show
            .Where(entry => entry.Type != OfficialRoomEntryData.RoomType || entry.Room is not null)
            .ToList();
    }

    /// <summary>The hand-picked promoted categories, each led by its first room.</summary>
    private async Task<List<PromotedRoomCategoryData>> PromotedCategoriesAsync(AppDbContext db, PlayerData player)
    {
        var population = await lister.PopulationAsync();
        var result = new List<PromotedRoomCategoryData>();
        foreach (var category in await db.PromotedRoomCategories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync())
        {
            var rooms = await db.Rooms.Include(r => r.Owner).AsNoTracking().Where(r => category.RoomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);
            var ordered = category.RoomIds.Where(rooms.ContainsKey).Take(options.Value.PromotedRoomsPerCategory).Select(id => rooms[id]).ToList();
            if (ordered.Count == 0)
                continue;

            var listed = await lister.ProjectAsync(db, ordered, player.Id, population);
            result.Add(new PromotedRoomCategoryData(category.Code, ordered[0].Owner?.Figure ?? string.Empty,
                listed.Select(GuestRoomData.From).ToList()));
        }

        return result;
    }

    public async Task SendCategoriesWithUserCountAsync(PlayerData player)
    {
        var population = await lister.PopulationAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var active = population.Where(p => p.Value > 0).Select(p => p.Key).ToList();
        var rooms = await db.Rooms.AsNoTracking().Where(r => active.Contains(r.Id))
            .Select(r => new { r.Id, r.CategoryId, r.MaxUsers }).ToListAsync();

        var categories = await catalog.GetFlatCategoriesAsync();
        await messenger.SendAsync(player.Id, new CategoriesWithVisitorCountComposer(categories
            .Select(c => new CategoryVisitorCountData(
                c.NodeId,
                rooms.Where(r => r.CategoryId == c.NodeId).Sum(r => population.GetValueOrDefault(r.Id)),
                rooms.Where(r => r.CategoryId == c.NodeId).Sum(r => r.MaxUsers)))
            .ToList()));
    }

    public async Task ConvertGlobalRoomIdAsync(PlayerData player, string globalId)
    {
        var prefix = options.Value.GlobalRoomIdPrefix;
        var local = globalId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? globalId[prefix.Length..] : globalId;
        var roomId = int.TryParse(local, CultureInfo.InvariantCulture, out var id) ? id : 0;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (roomId > 0 && !await db.Rooms.AnyAsync(r => r.Id == roomId))
            roomId = 0;
        await messenger.SendAsync(player.Id, new ConvertedRoomIdComposer(globalId, roomId));
    }

    public async Task ForwardToSomeRoomAsync(PlayerData player, string type)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        int? roomId;
        if (type == RandomFriendingRoom)
        {
            // somewhere people are, open to anyone, and not the player's own
            var population = await lister.PopulationAsync();
            var busy = population.Where(p => p.Value > 0).Select(p => p.Key).ToList();
            var candidates = await db.Rooms.AsNoTracking()
                .Where(r => busy.Contains(r.Id) && r.State == RoomState.Open && r.OwnerId != player.Id)
                .Select(r => new { r.Id, r.MaxUsers }).ToListAsync();
            var open = candidates.Where(r => population.GetValueOrDefault(r.Id) < r.MaxUsers).ToList();
            roomId = open.Count > 0 ? open[Random.Shared.Next(open.Count)].Id : null;
        }
        else
        {
            roomId = await db.ForwardRooms.Where(f => f.Type == type).Select(f => (int?)f.RoomId).FirstOrDefaultAsync();
        }

        if (roomId is { } forward)
            await messenger.SendAsync(player.Id, new RoomForwardMessageComposer(forward));
    }

    public async Task ForwardToRandomPromotedRoomAsync(PlayerData player, string categoryCode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rooms = await db.PromotedRoomCategories.AsNoTracking()
            .Where(c => c.Code == categoryCode).Select(c => c.RoomIds).FirstOrDefaultAsync();
        if (rooms is { Count: > 0 })
            await messenger.SendAsync(player.Id, new RoomForwardMessageComposer(rooms[Random.Shared.Next(rooms.Count)]));
    }

    public async Task OpenRoomNetworkAsync(PlayerData player, int networkId, int homeRoomId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var network = await db.RoomNetworks.AsNoTracking().FirstOrDefaultAsync(n => n.Id == networkId);
        var population = await lister.PopulationAsync();
        var capacity = network is null
            ? []
            : await db.Rooms.AsNoTracking().Where(r => network.RoomIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.MaxUsers);

        // the network's first room with space; when all are full, home instead
        var roomId = network?.RoomIds.FirstOrDefault(id => capacity.TryGetValue(id, out var max) && population.GetValueOrDefault(id) < max) ?? 0;
        if (roomId == 0)
            roomId = homeRoomId;

        await messenger.SendAsync(player.Id, roomId > 0
            ? new RoomForwardMessageComposer(roomId)
            : new CantConnectMessageComposer(CantConnectMessageComposer.RoomFull, null));
    }

    public async Task SendCanCreateRoomAsync(PlayerData player)
    {
        var (owned, limit) = await RoomCountAsync(player);
        await messenger.SendAsync(player.Id, new CanCreateRoomComposer(
            owned < limit ? CanCreateRoomComposer.Allowed : CanCreateRoomComposer.LimitReached, limit));
    }

    public async Task<bool> EnsureCanCreateRoomAsync(PlayerData player)
    {
        var (owned, limit) = await RoomCountAsync(player);
        if (owned < limit)
            return true;

        await messenger.SendAsync(player.Id, new CanCreateRoomComposer(CanCreateRoomComposer.LimitReached, limit));
        return false;
    }

    private async Task<(int Owned, int Limit)> RoomCountAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var owned = await db.Rooms.CountAsync(r => r.OwnerId == player.Id);
        return (owned, player.ClubLevel > ClubLevel.None ? roomOptions.Value.ClubRoomLimit : roomOptions.Value.RoomLimit);
    }

    public async Task ToggleStaffPickAsync(PlayerData player, int roomId, bool staffPick)
    {
        if (!permissions.Has(player, PermissionNames.NavigatorStaffPick))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Rooms.Where(r => r.Id == roomId).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsStaffPick, staffPick)) == 0)
            return;

        await grains.GetGrain<IGameRoomGrain>(roomId).StaffPickChangedAsync(staffPick);

        // the owner's rooms picked by staff
        var ownerId = await db.Rooms.Where(r => r.Id == roomId).Select(r => r.OwnerId).FirstAsync();
        var picked = await db.Rooms.CountAsync(r => r.OwnerId == ownerId && r.IsStaffPick);
        await grains.GetGrain<IPlayerGrain>(ownerId).ReachAchievementAsync(AchievementNames.StaffPickedRooms, picked);
    }

    public async Task UpdateThumbnailAsync(PlayerData player, int roomId, int backgroundImageId, int frontImageId, int foregroundImageId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var updated = await db.Rooms
            .Where(r => r.Id == roomId && (r.OwnerId == player.Id || permissions.Has(player, PermissionNames.NavigatorStaffPick)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ThumbnailBackgroundImageId, backgroundImageId)
                .SetProperty(r => r.ThumbnailFrontImageId, frontImageId)
                .SetProperty(r => r.ThumbnailForegroundImageId, foregroundImageId));

        await messenger.SendAsync(player.Id, new RoomThumbnailUpdateResultComposer(roomId,
            updated > 0 ? RoomThumbnailUpdateResultComposer.Updated : RoomThumbnailUpdateResultComposer.NotAllowed));
    }

    public async Task RecordPromotionsTabViewedAsync(PlayerData player)
    {
        if (!_recentInteractions.FirstInWindow((player.Id, RoomAdInteractionKind.TabViewed, null)))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomAdInteractions.Add(new RoomAdInteraction { UserId = player.Id, Kind = RoomAdInteractionKind.TabViewed });
        await db.SaveChangesAsync();
    }

    public async Task RecordPromotionClickedAsync(PlayerData player, int roomId)
    {
        if (!_recentInteractions.FirstInWindow((player.Id, RoomAdInteractionKind.AdClicked, roomId)))
            return;

        var now = DateTimeOffset.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync();
        var eventId = await db.RoomEvents.Where(e => e.RoomId == roomId && e.ExpiresAt > now)
            .OrderByDescending(e => e.CreatedAt).Select(e => (int?)e.Id).FirstOrDefaultAsync();
        if (eventId is null)
            return;

        db.RoomAdInteractions.Add(new RoomAdInteraction
        {
            UserId = player.Id, Kind = RoomAdInteractionKind.AdClicked, EventId = eventId, RoomId = roomId,
        });
        await db.SaveChangesAsync();
    }
}
