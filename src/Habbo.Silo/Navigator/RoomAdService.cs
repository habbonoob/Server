using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Habbo.Silo.Chat;
using Habbo.Silo.Users;
using Habbo.Silo.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;

namespace Habbo.Silo.Navigator;

/// <summary>Room ads. The offers are those on the catalog's room ad page; buying one is an ordinary
/// purchase (it may carry a badge), and gives the room its event for the ad's time — or, extending,
/// adds that time to the event it has.</summary>
public sealed class RoomAdService(
    IDbContextFactory<AppDbContext> dbFactory,
    IGrainFactory grains,
    IShopCatalog catalog,
    INavigatorCatalog navigator,
    PlayerMessenger messenger,
    WordFilter wordFilter,
    Habbo.Silo.Friends.FriendActivityNotifier friendActivity,
    IOptions<NavigatorOptions> options) : IRoomAdService
{
    /// <summary>Ad statistics are kept once per player, kind and room a minute: each is a packet the client sends on a
    /// click or a tab change, and a row for every one let a client write rows as fast as it could send.</summary>
    private readonly RecentlySeen<(Guid, RoomAdInteractionKind, int?)> _recentInteractions = new(TimeSpan.FromMinutes(1));

    /// <summary>What kind of event a room ad is.</summary>
    private const int RoomAdEventType = 1;

    public async Task SendPurchaseInfoAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rooms = await db.Rooms.AsNoTracking().Where(r => r.OwnerId == player.Id).OrderBy(r => r.Name)
            .Select(r => new RoomAdRoomData(r.Id, r.Name, db.RoomRights.Any(right => right.RoomId == r.Id)))
            .ToListAsync();
        await messenger.SendAsync(player.Id, new RoomAdPurchaseInfoComposer(player.ClubLevel == ClubLevel.Vip, rooms));
    }

    public async Task<PurchaseResult> PurchaseAsync(PlayerData player, int offerId, int roomId, string name, bool extended, string description, int categoryId)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > RoomEvent.NameMaxLength || description.Length > RoomEvent.DescriptionMaxLength
            || (await navigator.GetEventCategoriesAsync()).All(c => c.Id != categoryId || !c.Visible))
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        await using var db = await dbFactory.CreateDbContextAsync();
        var onAdPage = await db.CatalogListings.AnyAsync(l => l.OfferId == offerId && l.Page!.LayoutCode == options.Value.RoomAdPageLayout);
        if (!onAdPage || await catalog.GetOfferAsync(offerId) is not { } offer
            || !await db.Rooms.AnyAsync(r => r.Id == roomId && r.OwnerId == player.Id))
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var now = DateTimeOffset.UtcNow;
        var running = await db.RoomEvents.FirstOrDefaultAsync(e => e.RoomId == roomId && e.ExpiresAt > now);
        if (running is not null != extended)
            return PurchaseResult.Failed(PurchaseFailure.NotAllowed);

        var result = await grains.GetGrain<IPlayerGrain>(player.Id).PurchaseAsync(offer, 1, string.Empty);
        if (!result.Success)
            return result;

        name = (await wordFilter.FilterAsync(name, [])).Text;
        description = (await wordFilter.FilterAsync(description, [])).Text;
        var length = TimeSpan.FromMinutes(options.Value.RoomAdDurationMinutes);
        if (running is null)
        {
            db.RoomEvents.Add(new RoomEvent
            {
                RoomId = roomId, OwnerId = player.Id, Type = RoomAdEventType, CategoryId = categoryId,
                Name = name, Description = description, ExpiresAt = now + length,
            });
        }
        else
        {
            running.ExpiresAt += length;
            running.Name = name;
            running.Description = description;
            running.CategoryId = categoryId;
        }

        await db.SaveChangesAsync();
        await grains.GetGrain<IGameRoomGrain>(roomId).EventChangedAsync();
        if (running is null)
            await friendActivity.RoomEventStartedAsync(player.Id, name);
        return result;
    }

    public async Task RecordPurchaseInitiatedAsync(PlayerData player)
    {
        if (!_recentInteractions.FirstInWindow((player.Id, RoomAdInteractionKind.PurchaseInitiated, null)))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        db.RoomAdInteractions.Add(new RoomAdInteraction { UserId = player.Id, Kind = RoomAdInteractionKind.PurchaseInitiated });
        await db.SaveChangesAsync();
    }
}
